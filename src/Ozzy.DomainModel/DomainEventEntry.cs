namespace Ozzy.DomainModel
{
    /// <summary>
    /// ����� ��� ������������������� �������� DomainEventRecord ������ Disruptor
    /// </summary>
    public class DomainEventEntry
    {
        public IDomainEventRecord Value { get; set; }        
    }
}