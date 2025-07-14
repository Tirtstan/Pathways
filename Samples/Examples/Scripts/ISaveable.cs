namespace Pathways.Samples
{
    public interface ISaveable<T>
    {
        public T GetData();
        public void SetData(T saveData);
    }
}
