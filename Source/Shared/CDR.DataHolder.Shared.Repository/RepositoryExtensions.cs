using System.Linq.Expressions;

namespace CDR.DataHolder.Shared.Repository
{
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Extension add where filter if the condition is true
        /// </summary>
        /// <typeparam name="T">The entity type</typeparam>
        /// <param name="query">The entity querable</param>
        /// <param name="condition">The condition</param>
        /// <param name="predicate">The predicate</param>
        /// <returns><see cref="IQueryable<T>"/></returns>
        public static IQueryable<T> WhereIf<T>(this IQueryable<T> query, bool condition, Expression<Func<T, bool>> predicate)
        {
            if (condition)
                query = query.Where(predicate);

            return query;
        }

        public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> list, bool condition, Func<T, bool> predicate)
        {
            if (condition)
                list = list.Where(predicate);

            return list;
        }
    }
}
