using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.Core.Utils.DI;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Services;

[RegisterScoped(typeof(ICommentRepository))]
public class CommentRepository : Repository<NooTubeVideoCommentModel>, ICommentRepository;
