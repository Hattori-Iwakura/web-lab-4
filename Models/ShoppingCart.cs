namespace web_lab_4.Models
{
    public class ShoppingCart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        // Add item using Product object and quantity (used in controller)
        public void AddItem(Product product, int quantity = 1)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }
        }

        // Add item using CartItem object (keep for compatibility)
        public void AddItem(CartItem item)
        {
            var existingItem = Items.FirstOrDefault(i => i.ProductId == item.ProductId);
            if (existingItem != null)
                existingItem.Quantity += item.Quantity;
            else
                Items.Add(item);
        }

        // Update quantity for specific product (used in controller)
        public void UpdateQuantity(int productId, int quantity)
        {
            var item = Items.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                if (quantity <= 0)
                {
                    RemoveItem(productId);
                }
                else
                {
                    item.Quantity = quantity;
                }
            }
        }

        // Remove item by product ID
        public void RemoveItem(int productId)
        {
            Items.RemoveAll(i => i.ProductId == productId);
        }

        // Calculate total price
        public decimal TotalPrice()
        {
            return Items.Sum(i => i.Price * i.Quantity);
        }

        // Get total item count
        public int TotalItems()
        {
            return Items.Sum(i => i.Quantity);
        }

        // Clear all items
        public void Clear()
        {
            Items.Clear();
        }

        // Check if cart is empty
        public bool IsEmpty()
        {
            return !Items.Any();
        }
    }
}
