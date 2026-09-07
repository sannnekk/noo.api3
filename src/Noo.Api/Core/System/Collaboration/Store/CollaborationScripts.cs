namespace Noo.Api.Core.System.Collaboration.Store;

/// <summary>
/// The operations that have to be one step. Each of these is a read followed by a write whose
/// correctness depends on nothing having changed in between — taking a lease only if it is free,
/// releasing one only if it is still ours, replacing the log only if it is the length we read.
/// Split into separate round trips they would all be races between instances.
///
/// Every key a script touches belongs to one room, and a room's keys share a hash tag, so the
/// scripts that build key names from a prefix stay correct on a clustered Redis.
/// </summary>
public static class CollaborationScripts
{
    /// <summary>
    /// KEYS: lease, connection's lease set, room's leased-path set.
    /// ARGV: connectionId, lease ttl ms, path, room ttl ms.
    /// Returns [holderConnectionId, remaining ttl ms] — the caller compares the holder with
    /// itself to learn whether it won.
    /// </summary>
    public const string AcquireLease = """
        local function claim()
          redis.call('SADD', KEYS[2], ARGV[3])
          redis.call('PEXPIRE', KEYS[2], ARGV[4])
          redis.call('SADD', KEYS[3], ARGV[3])
          redis.call('PEXPIRE', KEYS[3], ARGV[4])
          return {ARGV[1], ARGV[2]}
        end
        if redis.call('SET', KEYS[1], ARGV[1], 'NX', 'PX', ARGV[2]) then
          return claim()
        end
        local holder = redis.call('GET', KEYS[1])
        if holder == ARGV[1] then
          redis.call('PSETEX', KEYS[1], ARGV[2], ARGV[1])
          return claim()
        end
        return {holder, tostring(redis.call('PTTL', KEYS[1]))}
        """;

    /// <summary>
    /// KEYS: lease, connection's lease set, room's leased-path set. ARGV: connectionId, path.
    /// The connection's own entry goes either way: if the lease is no longer ours, it is stale.
    /// </summary>
    public const string ReleaseLease = """
        redis.call('SREM', KEYS[2], ARGV[2])
        if redis.call('GET', KEYS[1]) == ARGV[1] then
          redis.call('DEL', KEYS[1])
          redis.call('SREM', KEYS[3], ARGV[2])
          return 1
        end
        return 0
        """;

    /// <summary>
    /// KEYS: presence hash, presence seen zset, connection's lease set.
    /// ARGV: connectionId, now ms, lease ttl ms, lease key prefix, room ttl ms.
    /// Returns 0 when the member was already pruned, so the caller rejoins instead of
    /// heartbeating into an empty room.
    /// </summary>
    public const string Heartbeat = """
        if redis.call('HEXISTS', KEYS[1], ARGV[1]) == 0 then return 0 end
        redis.call('ZADD', KEYS[2], ARGV[2], ARGV[1])
        redis.call('PEXPIRE', KEYS[1], ARGV[5])
        redis.call('PEXPIRE', KEYS[2], ARGV[5])
        local paths = redis.call('SMEMBERS', KEYS[3])
        for i = 1, #paths do
          local key = ARGV[4] .. paths[i]
          if redis.call('GET', key) == ARGV[1] then
            redis.call('PEXPIRE', key, ARGV[3])
          else
            redis.call('SREM', KEYS[3], paths[i])
          end
        end
        redis.call('PEXPIRE', KEYS[3], ARGV[5])
        return 1
        """;

    /// <summary>
    /// KEYS: presence hash, presence seen zset, connection's lease set, room's leased-path set.
    /// ARGV: connectionId, lease key prefix. Returns the paths this connection freed.
    /// </summary>
    public const string Leave = """
        redis.call('HDEL', KEYS[1], ARGV[1])
        redis.call('ZREM', KEYS[2], ARGV[1])
        local paths = redis.call('SMEMBERS', KEYS[3])
        local freed = {}
        for i = 1, #paths do
          local key = ARGV[2] .. paths[i]
          if redis.call('GET', key) == ARGV[1] then
            redis.call('DEL', key)
            redis.call('SREM', KEYS[4], paths[i])
            freed[#freed + 1] = paths[i]
          end
        end
        redis.call('DEL', KEYS[3])
        return freed
        """;

    /// <summary>
    /// KEYS: ops list, seq base. ARGV: room ttl ms, then one entry per operation.
    /// Returns the sequence number of the last operation appended.
    /// </summary>
    public const string AppendOps = """
        for i = 2, #ARGV do redis.call('RPUSH', KEYS[1], ARGV[i]) end
        redis.call('PEXPIRE', KEYS[1], ARGV[1])
        redis.call('PEXPIRE', KEYS[2], ARGV[1])
        return tonumber(redis.call('GET', KEYS[2]) or '0') + redis.call('LLEN', KEYS[1])
        """;

    /// <summary>
    /// KEYS: ops list, seq base. ARGV: expected seq, room ttl ms, then one entry per operation.
    /// The base absorbs the operations dropped, so sequence numbers carry on rather than
    /// restarting under clients that already applied them.
    /// </summary>
    public const string CompactOps = """
        local base = tonumber(redis.call('GET', KEYS[2]) or '0')
        local len = redis.call('LLEN', KEYS[1])
        if base + len ~= tonumber(ARGV[1]) then return 0 end
        redis.call('DEL', KEYS[1])
        for i = 3, #ARGV do redis.call('RPUSH', KEYS[1], ARGV[i]) end
        redis.call('SET', KEYS[2], base + len - redis.call('LLEN', KEYS[1]))
        redis.call('PEXPIRE', KEYS[1], ARGV[2])
        redis.call('PEXPIRE', KEYS[2], ARGV[2])
        return 1
        """;

    /// <summary>KEYS: ops list, seq base, version. Returns the new version.</summary>
    public const string ClearDraft = """
        redis.call('DEL', KEYS[1])
        redis.call('DEL', KEYS[2])
        return redis.call('INCR', KEYS[3])
        """;

    /// <summary>
    /// KEYS: scope member set, scope seed flag. ARGV: connectionId, room ttl ms.
    /// Returns [maySeed, memberCount]. Only the first client into an empty scope is told to
    /// seed; two clients writing the starting content into one CRDT document is how a
    /// collaborative editor ends up showing everything twice.
    /// </summary>
    public const string JoinScope = """
        redis.call('SADD', KEYS[1], ARGV[1])
        redis.call('PEXPIRE', KEYS[1], ARGV[2])
        local count = redis.call('SCARD', KEYS[1])
        local claimed = redis.call('SET', KEYS[2], ARGV[1], 'NX', 'PX', ARGV[2])
        if claimed and count == 1 then return {1, count} end
        return {0, count}
        """;

    /// <summary>
    /// KEYS: scope member set, scope seed flag. ARGV: connectionId.
    /// Clearing the seed claim with the last member is what lets the next arrival start the
    /// document again from what was saved.
    /// </summary>
    public const string LeaveScope = """
        redis.call('SREM', KEYS[1], ARGV[1])
        if redis.call('SCARD', KEYS[1]) == 0 then
          redis.call('DEL', KEYS[1])
          redis.call('DEL', KEYS[2])
        end
        return 1
        """;
}
