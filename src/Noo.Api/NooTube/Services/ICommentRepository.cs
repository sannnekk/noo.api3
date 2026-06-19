using Noo.Api.Core.DataAbstraction.Db;
using Noo.Api.NooTube.Models;

namespace Noo.Api.NooTube.Services;

public interface ICommentRepository : IRepository<NooTubeVideoCommentModel>;
