using InteractiveStories;

namespace TestConsole
{

	/// <summary>
	/// Punto di ingresso della console.
	/// Per eseguire un esempio diverso, sostituire la chiamata in Main con la relativa classe Execute().
	/// </summary>
	internal class Program
	{
		/// <summary>
		/// Avvia l'esempio selezionato.
		/// </summary>
		private static void Main(string[] args)
		{
			MicroLLM.Execute();
			// DocumentTest.Execute();
		}
	}
}