using UnityEngine;

namespace Pathways.Samples
{
    public class Item : MonoBehaviour, ISaveable<ItemData>
    {
        [Header("Item Data")]
        [SerializeField]
        private ItemData itemData;

        public virtual void RandomiseProperties()
        {
            itemData = new ItemData(transform.position.x, transform.position.y, itemData.ItemName, itemData.Quantity);
        }

        public void SetData(ItemData saveData) => itemData = saveData;

        public ItemData GetData() => itemData;
    }
}
