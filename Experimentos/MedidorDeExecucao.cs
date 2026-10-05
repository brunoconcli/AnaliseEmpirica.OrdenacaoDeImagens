namespace AnaliseEmpirica.OrdenacaoDeImagens.Experimentos;

using System.Diagnostics;

using AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;
using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>Resultado da medição de um algoritmo sobre uma entrada.</summary>
/// <param name="Ordenado">Saída da execução instrumentada, já conferida.</param>
/// <param name="TempoMs">Tempo médio de uma ordenação, em milissegundos.</param>
/// <param name="ExecucoesCronometradas">Quantas ordenações foram cronometradas para obter a média de tempo.</param>
public sealed record MedicaoOrdenacao<T>(T[] Ordenado, long Comparacoes, long Movimentacoes, double TempoMs, int ExecucoesCronometradas);

/// <summary>
/// Mede um algoritmo sobre uma entrada, seguindo docs/metodologia.md (seção 3):
/// uma execução instrumentada para as contagens e outra, sem instrumentação de
/// comparações, para o tempo.
/// </summary>
public static class MedidorDeExecucao
{
  /// <summary>Tempo mínimo acumulado na execução de tempo, para que ordenações curtas sejam medidas com precisão.</summary>
  public static readonly TimeSpan TempoMinimoPadrao = TimeSpan.FromMilliseconds(2);

  private const int MaximoExecucoesCronometradas = 10_000;

  /// <exception cref="InvalidOperationException">Se a saída do algoritmo não estiver ordenada.</exception>
  public static MedicaoOrdenacao<T> Medir<T>(
      IAlgoritmoOrdenacao algoritmo, T[] entrada, IComparer<T> comparador, TimeSpan tempoMinimo)
  {
    ArgumentNullException.ThrowIfNull(algoritmo);
    ArgumentNullException.ThrowIfNull(entrada);
    ArgumentNullException.ThrowIfNull(comparador);

    // 1. Execução instrumentada: comparações e movimentações.
    var contadores = new Contadores();
    var ordenado = (T[])entrada.Clone();
    algoritmo.Ordenar(ordenado, new ContadorComparer<T>(comparador, contadores), contadores);
    VerificarOrdenacao(algoritmo, ordenado, comparador);

    // 2. Execução de tempo: comparador original, sem o envoltório de contagem.
    var (tempoMs, execucoes) = MedirTempo(algoritmo, entrada, comparador, tempoMinimo);

    return new MedicaoOrdenacao<T>(ordenado, contadores.Comparacoes, contadores.Movimentacoes, tempoMs, execucoes);
  }

  /// <summary>
  /// Repete a ordenação, sempre sobre uma cópia nova da entrada, até acumular o tempo mínimo.
  /// A cópia fica fora do trecho cronometrado. Retorna o tempo médio por ordenação.
  /// </summary>
  private static (double TempoMs, int Execucoes) MedirTempo<T>(
      IAlgoritmoOrdenacao algoritmo, T[] entrada, IComparer<T> comparador, TimeSpan tempoMinimo)
  {
    long ticksMinimos = (long)(tempoMinimo.TotalSeconds * Stopwatch.Frequency);
    var copia = new T[entrada.Length];
    var contadoresDescartados = new Contadores();
    long ticksAcumulados = 0;
    int execucoes = 0;

    // Reduz a chance de o coletor de lixo interromper a medição com sobras de execuções anteriores.
    GC.Collect();
    GC.WaitForPendingFinalizers();
    GC.Collect();

    do
    {
      Array.Copy(entrada, copia, entrada.Length);

      long inicio = Stopwatch.GetTimestamp();
      algoritmo.Ordenar(copia, comparador, contadoresDescartados);
      ticksAcumulados += Stopwatch.GetTimestamp() - inicio;

      execucoes++;
    }
    while (ticksAcumulados < ticksMinimos && execucoes < MaximoExecucoesCronometradas);

    double tempoMs = ticksAcumulados * 1000.0 / Stopwatch.Frequency / execucoes;
    return (tempoMs, execucoes);
  }

  private static void VerificarOrdenacao<T>(IAlgoritmoOrdenacao algoritmo, T[] vetor, IComparer<T> comparador)
  {
    for (int i = 1; i < vetor.Length; i++)
    {
      if (comparador.Compare(vetor[i - 1], vetor[i]) > 0)
      {
        throw new InvalidOperationException($"{algoritmo.Nome} produziu uma saída fora de ordem na posição {i}.");
      }
    }
  }
}
