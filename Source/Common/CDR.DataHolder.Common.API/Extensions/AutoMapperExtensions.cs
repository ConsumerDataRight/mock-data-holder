using AutoMapper;

namespace CDR.DataHolder.Common.API.Extensions
{
    public static class AutoMapperExtensions
    {
        public static IMappingExpression<TSource, TDestination> IgnoreAllMembers<TSource, TDestination>(this IMappingExpression<TSource, TDestination> expr)
        {
            var destinationType = typeof(TDestination);

            foreach (var property in destinationType.GetProperties())
            {
                expr.ForMember(property.Name, opt => opt.Ignore());
            }

            return expr;
        }
    }
}
