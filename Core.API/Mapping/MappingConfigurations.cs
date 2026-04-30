using Common.Helpers;
using Domain.DTO;
using Domain.Responses;
using Mapster;
using MapsterMapper;
using System.Reflection;

namespace API.Mapping
{
    public static class MappingConfigurations
    {
        public static IServiceCollection ConfigureMapster(this IServiceCollection services)
        {
            services.AddMapster();
            services.AddSingleton<IMapper, Mapper>();
            AddMappings();
            TypeAdapterConfig.GlobalSettings.Scan(Assembly.GetExecutingAssembly());
            return services;
        }

        private static void AddMappings()
        {
            TypeAdapterConfig<BestStoryResponse, StoryDetailDTO>
                .NewConfig()
                .Map(dest => dest.CommentCount, src => CommentsHelper.CountComments(src.Kids))
                .Map(dest => dest.PostedBy, src => src.By)
                .Map(dest => dest.Time, src => DateHelper.ConvertUnixTimeToDateTime(src.Time))
                .Map(dest => dest.Uri, src => src.Url);
        }
    }
}
