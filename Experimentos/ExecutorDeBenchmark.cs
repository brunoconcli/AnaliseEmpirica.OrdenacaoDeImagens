namespace AnaliseEmpirica.OrdenacaoDeImagens.Experimentos;

using AnaliseEmpirica.OrdenacaoDeImagens.Amostragem;
using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Executa a bateria de experimentos: para cada tamanho e repetição sorteia uma amostra,
/// monta os casos de entrada e mede todos os algoritmos sobre o mesmo vetor.
/// </summary>
/// <remarks>
/// Os resultados são produzidos sob demanda (<c>yield</c>), para que quem consome possa
/// gravá-los e mostrar o progresso à medida que cada execução termina.
/// </remarks>
public sealed class ExecutorDeBenchmark
{
  private const int TamanhoAquecimento = 100;
  private const int SementeAquecimento = 0;

  private readonly ConfiguracaoBenchmark _configuracao;
  private readonly TimeSpan _tempoMinimo;

  public ExecutorDeBenchmark(ConfiguracaoBenchmark configuracao, TimeSpan? tempoMinimo = null)
  {
    ArgumentNullException.ThrowIfNull(configuracao);

    _configuracao = configuracao;
    _tempoMinimo = tempoMinimo ?? MedidorDeExecucao.TempoMinimoPadrao;
  }

  public IEnumerable<ResultadoExecucao> Executar(IReadOnlyList<ItemImagem> itens)
  {
    ArgumentNullException.ThrowIfNull(itens);
    if (_configuracao.Tamanhos.Any(n => n > itens.Count))
    {
      throw new ArgumentException($"Há tamanhos maiores que o número de imagens disponíveis ({itens.Count}).", nameof(itens));
    }

    Aquecer(itens);

    var comparador = _configuracao.Comparador;
    string criterio = _configuracao.DescricaoCriterio;

    foreach (int tamanho in _configuracao.Tamanhos)
    {
      for (int repeticao = 1; repeticao <= _configuracao.Repeticoes; repeticao++)
      {
        int semente = _configuracao.Semente(tamanho, repeticao);
        int[] indices = GeradorDeAmostras.SortearIndices(itens.Count, tamanho, new Random(semente));

        foreach (var caso in _configuracao.Casos)
        {
          // A mesma entrada é entregue a todos os algoritmos (delineamento pareado).
          var entrada = GeradorDeAmostras.MontarCaso(
              itens, indices, caso, comparador, _configuracao.FracaoDesordem, new Random(semente + 1));

          foreach (var algoritmo in _configuracao.Algoritmos)
          {
            var medicao = MedidorDeExecucao.Medir(algoritmo, entrada, comparador, _tempoMinimo);

            yield return new ResultadoExecucao(
                algoritmo.Nome,
                criterio,
                caso,
                tamanho,
                repeticao,
                semente,
                medicao.TempoMs,
                medicao.Comparacoes,
                medicao.Movimentacoes);
          }
        }
      }
    }
  }

  /// <summary>
  /// Executa cada algoritmo uma vez sobre uma amostra pequena e descarta o resultado,
  /// para que o JIT compile o código antes das medições.
  /// </summary>
  private void Aquecer(IReadOnlyList<ItemImagem> itens)
  {
    int tamanho = Math.Min(TamanhoAquecimento, itens.Count);
    int[] indices = GeradorDeAmostras.SortearIndices(itens.Count, tamanho, new Random(SementeAquecimento));
    var amostra = indices.Select(i => itens[i]).ToArray();

    foreach (var algoritmo in _configuracao.Algoritmos)
    {
      MedidorDeExecucao.Medir(algoritmo, amostra, _configuracao.Comparador, _tempoMinimo);
    }
  }
}
