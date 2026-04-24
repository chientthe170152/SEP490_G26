namespace MTCA.Domain.Common;

public interface IConcurrencyAware
{
    byte[] RowVersion { get; set; }
}
