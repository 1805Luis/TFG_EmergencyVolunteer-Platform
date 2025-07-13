namespace UserService.Data
{
    public class User
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string? Telf { get; set; }

        public string? Disponibilidad { get; set; }

        public string? Habilidades { get; set; }
    }
}
