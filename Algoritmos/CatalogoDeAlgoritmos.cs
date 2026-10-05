namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

/// <summary>
/// Lista dos algoritmos disponíveis, com o nome curto usado na linha de comando.
/// </summary>
/// <remarks>
/// Os algoritmos não guardam estado entre execuções, então uma única instância
/// de cada pode ser reutilizada.
/// </remarks>
public static class CatalogoDeAlgoritmos
{
  private static readonly Dictionary<string, IAlgoritmoOrdenacao> _porNome = new(StringComparer.OrdinalIgnoreCase)
  {
    ["insertion"] = new InsertionSort(),
    ["selection"] = new SelectionSort(),
    ["merge"] = new MergeSort(),
    ["heap"] = new HeapSort(),
    ["quick"] = new QuickSort(),
  };

  public static IReadOnlyCollection<string> Nomes => _porNome.Keys;

  public static IReadOnlyCollection<IAlgoritmoOrdenacao> Todos => _porNome.Values;

  public static bool TryObter(string nome, out IAlgoritmoOrdenacao algoritmo) =>
      _porNome.TryGetValue(nome, out algoritmo!);
}
