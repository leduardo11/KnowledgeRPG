namespace KnowledgeRPG
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
			using var game = new KnowledgeRPGGame();
			game.Run();
		}
    }
}
