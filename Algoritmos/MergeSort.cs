namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Merge Sort recursivo (top-down) com um único vetor auxiliar de tamanho n.
/// </summary>
/// <remarks>
/// Comparações: entre ~(n/2)·log₂n e ~n·log₂n em qualquer caso, O(n log n).
/// Movimentações: 2n por nível de recursão (cópia para o auxiliar e intercalação
/// de volta), totalizando ~2n·log₂n.
/// Memória extra: O(n).
/// Estável: em caso de empate, a intercalação sempre escolhe o elemento da metade esquerda.
/// </remarks>
public sealed class MergeSort : IAlgoritmoOrdenacao
{
  public string Nome => "Merge Sort";
  public bool Estavel => true;

  public void Ordenar<T>(T[] vetor, IComparer<T> comparador, Contadores contadores)
  {
    ArgumentNullException.ThrowIfNull(vetor);
    ArgumentNullException.ThrowIfNull(comparador);
    ArgumentNullException.ThrowIfNull(contadores);

    if (vetor.Length < 2)
    {
      return;
    }

    // Alocado uma única vez para não somar o custo de alocação a cada intercalação.
    var auxiliar = new T[vetor.Length];
    OrdenarIntervalo(vetor, auxiliar, 0, vetor.Length - 1, comparador, contadores);
  }

  private static void OrdenarIntervalo<T>(
      T[] vetor, T[] auxiliar, int inicio, int fim, IComparer<T> comparador, Contadores contadores)
  {
    if (inicio >= fim)
    {
      return;
    }

    int meio = inicio + (fim - inicio) / 2;
    OrdenarIntervalo(vetor, auxiliar, inicio, meio, comparador, contadores);
    OrdenarIntervalo(vetor, auxiliar, meio + 1, fim, comparador, contadores);
    Intercalar(vetor, auxiliar, inicio, meio, fim, comparador, contadores);
  }

  private static void Intercalar<T>(
      T[] vetor, T[] auxiliar, int inicio, int meio, int fim, IComparer<T> comparador, Contadores contadores)
  {
    for (int k = inicio; k <= fim; k++)
    {
      auxiliar[k] = vetor[k];
    }
    contadores.RegistrarMovimentacoes(fim - inicio + 1);

    int esquerda = inicio;
    int direita = meio + 1;

    for (int k = inicio; k <= fim; k++)
    {
      if (esquerda > meio)
      {
        vetor[k] = auxiliar[direita++];
      }
      else if (direita > fim)
      {
        vetor[k] = auxiliar[esquerda++];
      }
      else if (comparador.Compare(auxiliar[direita], auxiliar[esquerda]) < 0)
      {
        vetor[k] = auxiliar[direita++];
      }
      else
      {
        vetor[k] = auxiliar[esquerda++];
      }

      contadores.RegistrarMovimentacao();
    }
  }
}
