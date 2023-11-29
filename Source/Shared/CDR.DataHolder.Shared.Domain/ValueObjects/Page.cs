using System.Collections;

namespace CDR.DataHolder.Shared.Domain.ValueObjects
{
    public class Page<T> where T : IEnumerable
	{
		public T Data { get; set; } = default(T)!;

        public int CurrentPage { get; set; }
		public int PageSize { get; set; }

		public int TotalRecords { get; set; }
		public int TotalPages
		{
			get
			{
				if (TotalRecords == 0 || PageSize == 0)
				{
					return 0;
				}

				return (int)Math.Ceiling((decimal)TotalRecords / (decimal)PageSize);
			}
		}
	}
}
