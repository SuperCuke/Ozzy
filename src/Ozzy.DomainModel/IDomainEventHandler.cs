namespace Ozzy.DomainModel
{
    /// <summary>
    /// ���������� ��������� �������
    /// </summary>
    public interface IDomainEventHandler
    {
        /// <summary>
        /// ������������ �������� �������
        /// </summary>
        /// <param name="record"></param>
        void HandleEvent(DomainEventRecord record);
    }
}