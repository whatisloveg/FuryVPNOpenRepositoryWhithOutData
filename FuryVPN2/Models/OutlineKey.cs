namespace FuryVPN2.Models
{
    public class OutlineKey
    {
        //как переименовать?
        public int Id { get; set; }
        public string Name { get; set; }
        public string IdOnServer { get; set; }
        public string AccessUrl { get; set; }
        public int ServerId { get; set; }
        public bool IsDelete { get; set; } = false;
    }
}
