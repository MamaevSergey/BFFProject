using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderService.Models
{
    public class Order
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; } // Связь с пользователем (но просто как число, без внешних ключей SQL!)

        public int ProductId { get; set; } // ID товара
        public string ProductName { get; set; } // Копируем имя товара (деномализация), чтобы не зависеть от ProductService
        public double Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Время заказа
    }
}