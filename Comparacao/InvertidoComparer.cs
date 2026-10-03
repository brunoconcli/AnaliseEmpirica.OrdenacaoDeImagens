namespace AnaliseEmpirica.OrdenacaoDeImagens.Comparacao;

/// <summary>
/// Inverte o sentido de qualquer comparador, produzindo a ordem decrescente.
/// </summary>
/// <remarks>
/// Inverter o comparador, em vez de ordenar crescente e inverter o vetor no fim,
/// preserva a estabilidade: elementos equivalentes mantêm a ordem original
/// também na ordenação decrescente.
/// </remarks>
public sealed class InvertidoComparer<T> : IComparer<T>
{
  private readonly IComparer<T> _comparadorInterno;

  public InvertidoComparer(IComparer<T> comparadorInterno)
  {
    ArgumentNullException.ThrowIfNull(comparadorInterno);

    _comparadorInterno = comparadorInterno;
  }

  public int Compare(T? x, T? y) => _comparadorInterno.Compare(y, x);

  public override string ToString() => $"{_comparadorInterno} (decrescente)";
}
