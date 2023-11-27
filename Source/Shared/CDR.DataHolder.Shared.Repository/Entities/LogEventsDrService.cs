using System.ComponentModel.DataAnnotations;

namespace CDR.DataHolder.Shared.Repository.Entities
{
    public class LogEventsDrService
    {
        [Key]
        public int Id { get; set; }

        public string? Message { get; set; }

        public string? Level { get; set; }

        public DateTime TimeStamp { get; set; }

        public string? Exception { get; set; }

        [MaxLength(50)]
        public string? Environment { get; set; }

        [MaxLength(50)]
        public string? ProcessId { get; set; }

        [MaxLength(50)]
        public string? ProcessName { get; set; }

        [MaxLength(50)]
        public string? ThreadId { get; set; }

        [MaxLength(50)]
        public string? MethodName { get; set; }

        [MaxLength(100)]
        public string? SourceContext { get; set; }
    }
}