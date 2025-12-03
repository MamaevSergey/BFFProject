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
        public int UserId { get; set; }

        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public double Price { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}