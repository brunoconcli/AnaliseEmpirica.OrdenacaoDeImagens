namespace AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Envolve qualquer comparador e registra cada comparação em <see cref="Contadores"/>.
/// </summary>
/// <remarks>
/// Centralizar a contagem aqui garante que todos os algoritmos sejam medidos pela
/// mesma regra, sem depender de cada implementação lembrar de contar.
/// Como o envoltório adiciona uma chamada extra por comparação, as execuções que
/// medem tempo devem usar o comparador original, sem este envoltório.
/// </remarks>
public sealed class ComparadorContador<T> : IComparer<T>
{
  private readonly IComparer<T> _comparadorInterno;
  private readonly Contadores _contadores;

  public ComparadorContador(IComparer<T> comparadorInterno, Contadores contadores)
  {
    ArgumentNullException.ThrowIfNull(comparadorInterno);
    ArgumentNullException.ThrowIfNull(contadores);

    _comparadorInterno = comparadorInterno;
    _contadores = contadores;
  }

  public int Compare(T? x, T? y)
  {
    _contadores.RegistrarComparacao();
    return _comparadorInterno.Compare(x, y);
  }
}
