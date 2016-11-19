namespace Ozzy.DomainModel
{
    /// <summary>
    /// ������ ������ � �������� �������. ������ ��� ���������� �������� � ������������������ �������� �������, 
    /// ������� ����� ���������� ��-�� ������������ ���������� identity � SqlServer
    /// </summary>
    public class EmptyEventRecord : DomainEventRecord
    {
        public EmptyEventRecord(long sequence)
        {
            Sequence = sequence;
        }        
    }
}