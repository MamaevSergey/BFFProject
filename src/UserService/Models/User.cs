using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Models
{
    // Этот класс превратится в таблицу "Users" в базе данных
    public class User
    {
        [Key] // Это первичный ключ (ID)
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)] // База сама будет генерировать 1, 2, 3...
        public int Id { get; set; }

        [Required] // Обязательное поле
        public required string Name { get; set; }

        [Required]
        public required string Email { get; set; }

        // Пароли хранить в открытом виде нельзя, но для учебного примера пока оставим так
        public required string Password { get; set; }
    }
}