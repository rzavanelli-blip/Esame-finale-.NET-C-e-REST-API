namespace Esame_C__e_Reset_Api.Modelli
{
    public class Studente
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public string Cognome { get; set; }
        public List<Corso> CorsiIscritti { get; set; } = new List<Corso>();
    }
}
