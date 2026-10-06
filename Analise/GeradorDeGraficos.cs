namespace AnaliseEmpirica.OrdenacaoDeImagens.Analise;

using AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

using ScottPlot;
using ScottPlot.TickGenerators;

/// <summary>
/// Gera os gráficos de um benchmark a partir do resumo (média, desvio e mediana por grupo).
/// </summary>
/// <remarks>
/// Eixos logarítmicos são feitos plotando log₁₀ dos valores e rotulando as marcações
/// com os valores originais. Em escala log-log, crescimento n^k vira uma reta de inclinação k.
/// </remarks>
public sealed class GeradorDeGraficos
{
  private const int LarguraPainel = 800;
  private const int AlturaPainel = 560;
  private const float EspessuraLinha = 2;
  private const float TamanhoMarcador = 9;

  private readonly IReadOnlyList<LinhaResumo> _resumo;
  private readonly IReadOnlyList<string> _algoritmos;
  private readonly IReadOnlyList<CasoDeEntrada> _casos;
  private readonly IReadOnlyList<int> _tamanhos;

  public GeradorDeGraficos(IReadOnlyList<LinhaResumo> resumo)
  {
    ArgumentNullException.ThrowIfNull(resumo);
    if (resumo.Count == 0)
    {
      throw new ArgumentException("O resumo está vazio.", nameof(resumo));
    }

    _resumo = resumo;
    _algoritmos = [.. resumo.Select(l => l.Algoritmo).Distinct()];
    _casos = [.. resumo.Select(l => l.Caso).Distinct().Order()];
    _tamanhos = [.. resumo.Select(l => l.TamanhoAmostra).Distinct().Order()];
  }

  public string Criterio => _resumo[0].Criterio;

  public int MaiorTamanho => _tamanhos[^1];

  /// <summary>Comparações × n em log-log, um painel por caso de entrada, mesma escala em todos.</summary>
  public void SalvarComparacoesPorCaso(string caminhoPng) =>
      SalvarPainelPorCaso(
          caminhoPng,
          "Comparações",
          linha => linha.ComparacoesMedia,
          desvio: null);

  /// <summary>
  /// Comparações × n em escala linear, um painel por caso. Mostra a forma do crescimento:
  /// n² vira parábola e n·log n, uma linha quase reta. As curvas quadráticas dominam a escala.
  /// </summary>
  public void SalvarComparacoesPorCasoLinear(string caminhoPng) =>
      SalvarPainelLinearPorCaso(caminhoPng, "Comparações", linha => linha.ComparacoesMedia, somenteSubquadraticos: false);

  /// <summary>
  /// Como <see cref="SalvarComparacoesPorCasoLinear"/>, mas omitindo em cada painel as séries
  /// quadráticas, para que a forma das curvas n·log n fique visível.
  /// </summary>
  public void SalvarComparacoesPorCasoLinearSubquadraticos(string caminhoPng) =>
      SalvarPainelLinearPorCaso(caminhoPng, "Comparações", linha => linha.ComparacoesMedia, somenteSubquadraticos: true);

  /// <summary>Tempo × n em log-log (média ± desvio padrão), um painel por caso de entrada.</summary>
  public void SalvarTempoPorCaso(string caminhoPng) =>
      SalvarPainelPorCaso(
          caminhoPng,
          "Tempo (ms)",
          linha => linha.TempoMsMedia,
          desvio: linha => linha.TempoMsDesvio);

  /// <summary>
  /// Um painel por algoritmo: comparações medidas no caso aleatório (pontos, com desvio padrão)
  /// sobre a curva teórica do caso médio (linha cinza).
  /// </summary>
  public void SalvarTeoriaVsPratica(string caminhoPng)
  {
    var algoritmos = _algoritmos.Where(a => ModelosTeoricos.TryObter(a, out _)).ToList();
    if (algoritmos.Count == 0 || !_casos.Contains(CasoDeEntrada.Aleatorio))
    {
      return;
    }

    var multiplot = new Multiplot();
    multiplot.AddPlots(algoritmos.Count);
    multiplot.Layout = new ScottPlot.MultiplotLayouts.Grid(1, algoritmos.Count);

    for (int i = 0; i < algoritmos.Count; i++)
    {
      string algoritmo = algoritmos[i];
      ModelosTeoricos.TryObter(algoritmo, out var modelo);
      var linhas = Linhas(algoritmo, CasoDeEntrada.Aleatorio);
      var (cor, marcador) = EstiloGraficos.DoAlgoritmo(algoritmo);

      double[] tamanhos = [.. linhas.Select(l => (double)l.TamanhoAmostra)];
      double[] medidas = [.. linhas.Select(l => l.ComparacoesMedia)];
      double razao = medidas[^1] / modelo.Comparacoes(tamanhos[^1]);

      var grafico = multiplot.GetPlot(i);
      EstiloGraficos.AplicarBase(
          grafico,
          $"{algoritmo}\nmedido ÷ teoria = {razao.ToString("0.00", EstiloGraficos.Cultura)} (n = {tamanhos[^1]:N0})",
          "n",
          i == 0 ? "Comparações (média)" : "");
      grafico.Axes.Title.Label.FontSize = 15;

      double[] curvaX = [.. Enumerable.Range(0, 100).Select(k => tamanhos[0] + (tamanhos[^1] - tamanhos[0]) * k / 99.0)];
      double[] curvaY = [.. curvaX.Select(modelo.Comparacoes)];
      var teoria = grafico.Add.ScatterLine(curvaX, curvaY, EstiloGraficos.Teoria);
      teoria.LineWidth = EspessuraLinha;
      teoria.LegendText = $"teoria: {modelo.Formula}";

      var barras = grafico.Add.ErrorBar(tamanhos, medidas, [.. linhas.Select(l => l.ComparacoesDesvio)]);
      barras.Color = cor;
      barras.LineWidth = 1;

      var pontos = grafico.Add.ScatterPoints(tamanhos, medidas, cor);
      pontos.MarkerShape = marcador;
      pontos.MarkerSize = TamanhoMarcador + 2;
      pontos.LegendText = "medido";

      grafico.Axes.Left.TickGenerator = new NumericAutomatic
      {
        LabelFormatter = valor => valor.ToString("N0", EstiloGraficos.Cultura),
      };
      grafico.Axes.Bottom.TickGenerator = MarcacoesNosTamanhos(log: false);
      EstiloGraficos.MostrarLegenda(grafico, Alignment.UpperLeft);
    }

    multiplot.SavePng(caminhoPng, 360 * algoritmos.Count, 520);
  }

  /// <summary>
  /// Mapa de calor algoritmo × caso com o tempo mediano no maior n.
  /// Cor em escala logarítmica (mais escuro = mais lento) e o valor escrito em cada célula.
  /// </summary>
  public void SalvarMapaDeCasos(string caminhoPng)
  {
    int n = MaiorTamanho;
    var grafico = new Plot();
    EstiloGraficos.AplicarBase(grafico, $"Tempo mediano por caso de entrada (n = {n:N0}) · mais escuro = mais lento", "", "");
    grafico.HideGrid();

    var valores = new Dictionary<(int Linha, int Coluna), double>();
    for (int l = 0; l < _algoritmos.Count; l++)
    {
      for (int c = 0; c < _casos.Count; c++)
      {
        var linha = _resumo.FirstOrDefault(r => r.Algoritmo == _algoritmos[l] && r.Caso == _casos[c] && r.TamanhoAmostra == n);
        if (linha is not null)
        {
          valores[(l, c)] = linha.TempoMsMediana;
        }
      }
    }

    double logMinimo = Math.Log10(valores.Values.Min());
    double logMaximo = Math.Log10(valores.Values.Max());

    foreach (var ((l, c), valor) in valores)
    {
      double fracao = logMaximo > logMinimo ? (Math.Log10(valor) - logMinimo) / (logMaximo - logMinimo) : 0;
      int passo = (int)Math.Round(fracao * (EstiloGraficos.RampaSequencial.Length - 1));

      // A primeira linha (algoritmo 0) fica no topo.
      double y = _algoritmos.Count - 1 - l;
      var celula = grafico.Add.Rectangle(c - 0.48, c + 0.48, y - 0.47, y + 0.47);
      celula.FillColor = EstiloGraficos.RampaSequencial[passo];
      celula.LineWidth = 0;

      var texto = grafico.Add.Text($"{valor.ToString("0.000", EstiloGraficos.Cultura)} ms", c, y);
      texto.Alignment = Alignment.MiddleCenter;
      texto.LabelFontSize = 16;
      texto.LabelFontColor = passo >= 7 ? Colors.White : EstiloGraficos.TextoPrincipal;
    }

    var colunas = new NumericManual();
    for (int c = 0; c < _casos.Count; c++)
    {
      colunas.AddMajor(c, NomeDoCaso(_casos[c]));
    }

    var linhasDoMapa = new NumericManual();
    for (int l = 0; l < _algoritmos.Count; l++)
    {
      linhasDoMapa.AddMajor(_algoritmos.Count - 1 - l, _algoritmos[l]);
    }

    grafico.Axes.Bottom.TickGenerator = colunas;
    grafico.Axes.Left.TickGenerator = linhasDoMapa;
    grafico.Axes.Bottom.TickLabelStyle.FontSize = 14;
    grafico.Axes.Left.TickLabelStyle.FontSize = 14;
    grafico.Axes.SetLimits(-0.5, _casos.Count - 0.5, -0.5, _algoritmos.Count - 0.5);

    grafico.SavePng(caminhoPng, 260 + 230 * _casos.Count, 140 + 90 * _algoritmos.Count);
  }

  private void SalvarPainelPorCaso(
      string caminhoPng, string rotuloY, Func<LinhaResumo, double> valor, Func<LinhaResumo, double>? desvio)
  {
    var multiplot = new Multiplot();
    multiplot.AddPlots(_casos.Count);
    int colunas = _casos.Count == 1 ? 1 : 2;
    multiplot.Layout = new ScottPlot.MultiplotLayouts.Grid((_casos.Count + colunas - 1) / colunas, colunas);

    // Mesma escala vertical em todos os painéis, para que os casos possam ser comparados entre si.
    double[] todos = [.. _resumo.Select(valor).Where(v => v > 0)];
    double minimoY = Math.Floor(Math.Log10(todos.Min()));
    double maximoY = Math.Ceiling(Math.Log10(todos.Max()));
    double minimoX = Math.Log10(_tamanhos[0]);
    double maximoX = Math.Log10(_tamanhos[^1]);
    double folgaX = (maximoX - minimoX) * 0.06 + 0.02;

    for (int i = 0; i < _casos.Count; i++)
    {
      var caso = _casos[i];
      var grafico = multiplot.GetPlot(i);
      EstiloGraficos.AplicarBase(grafico, NomeDoCaso(caso), "n (escala log)", $"{rotuloY} (escala log)");
      var series = new Dictionary<string, double[]>();

      foreach (string algoritmo in _algoritmos)
      {
        var linhas = Linhas(algoritmo, caso).Where(l => valor(l) > 0).ToList();
        if (linhas.Count == 0)
        {
          continue;
        }

        var (cor, marcador) = EstiloGraficos.DoAlgoritmo(algoritmo);
        double[] xs = [.. linhas.Select(l => Math.Log10(l.TamanhoAmostra))];
        double[] ys = [.. linhas.Select(l => Math.Log10(valor(l)))];
        series[algoritmo] = ys;

        if (desvio is not null)
        {
          // Em escala log, a barra ±desvio fica assimétrica: log(média + dp) e log(média − dp).
          var barras = grafico.Add.ErrorBar(xs, ys, new double[xs.Length]);
          barras.YErrorPositive = [.. linhas.Select(l => Math.Log10(valor(l) + desvio(l)) - Math.Log10(valor(l)))];
          barras.YErrorNegative = [.. linhas.Select(l =>
              Math.Log10(valor(l)) - Math.Log10(Math.Max(valor(l) - desvio(l), valor(l) / 10)))];
          barras.Color = cor;
          barras.LineWidth = 1;
        }

        var serie = grafico.Add.Scatter(xs, ys, cor);
        serie.LineWidth = EspessuraLinha;
        serie.MarkerShape = marcador;
        serie.MarkerSize = TamanhoMarcador;
        serie.LegendText = algoritmo;
      }

      grafico.Axes.Bottom.TickGenerator = MarcacoesNosTamanhos(log: true);
      grafico.Axes.Left.TickGenerator = new NumericAutomatic
      {
        MinorTickGenerator = new LogMinorTickGenerator(),
        IntegerTicksOnly = true,
        LabelFormatter = EstiloGraficos.FormatarPotenciaDeDez,
      };
      grafico.Axes.SetLimits(minimoX - folgaX, maximoX + folgaX, minimoY, maximoY);
      Anotar(grafico, DescreverSobreposicoes(series), maximoX + folgaX, minimoY);

      // Uma única legenda, no primeiro painel: as cores e formas são as mesmas em todos.
      if (i == 0)
      {
        EstiloGraficos.MostrarLegenda(grafico, Alignment.UpperLeft);
      }
      else
      {
        grafico.HideLegend();
      }
    }

    int linhasDoGrid = (_casos.Count + colunas - 1) / colunas;
    multiplot.SavePng(caminhoPng, LarguraPainel * colunas, AlturaPainel * linhasDoGrid);
  }

  /// <summary>
  /// Escala linear: os eixos começam em zero e mostram os valores como são, sem logaritmo.
  /// Com <paramref name="somenteSubquadraticos"/>, cada painel omite as séries cujo expoente
  /// ajustado passa de 1,5 (crescimento quadrático) e avisa quais foram omitidas.
  /// </summary>
  private void SalvarPainelLinearPorCaso(
      string caminhoPng, string rotuloY, Func<LinhaResumo, double> valor, bool somenteSubquadraticos)
  {
    const double LimiteExpoenteSubquadratico = 1.5;

    var multiplot = new Multiplot();
    multiplot.AddPlots(_casos.Count);
    int colunas = _casos.Count == 1 ? 1 : 2;
    multiplot.Layout = new ScottPlot.MultiplotLayouts.Grid((_casos.Count + colunas - 1) / colunas, colunas);
    double maximoX = _tamanhos[^1] * 1.04;

    for (int i = 0; i < _casos.Count; i++)
    {
      var caso = _casos[i];
      var grafico = multiplot.GetPlot(i);
      EstiloGraficos.AplicarBase(grafico, NomeDoCaso(caso), "n", $"{rotuloY} (escala linear)");

      var seriesLog = new Dictionary<string, double[]>();
      var maximosPorSerie = new Dictionary<string, double>();
      var omitidas = new List<string>();
      double maximoY = 0;

      foreach (string algoritmo in _algoritmos)
      {
        var linhas = Linhas(algoritmo, caso).Where(l => valor(l) > 0).ToList();
        if (linhas.Count == 0)
        {
          continue;
        }

        double[] xs = [.. linhas.Select(l => (double)l.TamanhoAmostra)];
        double[] ys = [.. linhas.Select(valor)];

        if (somenteSubquadraticos && linhas.Count >= 2
            && AjusteDeCurva.AjustarPotencia(xs, ys).Expoente > LimiteExpoenteSubquadratico)
        {
          omitidas.Add(algoritmo);
          continue;
        }

        var (cor, marcador) = EstiloGraficos.DoAlgoritmo(algoritmo);
        var serie = grafico.Add.Scatter(xs, ys, cor);
        serie.LineWidth = EspessuraLinha;
        serie.MarkerShape = marcador;
        serie.MarkerSize = TamanhoMarcador;
        serie.LegendText = algoritmo;

        seriesLog[algoritmo] = [.. ys.Select(Math.Log10)];
        maximosPorSerie[algoritmo] = ys.Max();
        maximoY = Math.Max(maximoY, ys.Max());
      }

      var formatador = (double v) => v.ToString("N0", EstiloGraficos.Cultura);
      grafico.Axes.Bottom.TickGenerator = new NumericAutomatic { LabelFormatter = formatador };
      grafico.Axes.Left.TickGenerator = new NumericAutomatic { LabelFormatter = formatador };
      grafico.Axes.SetLimits(0, maximoX, 0, maximoY * 1.06);

      // Na escala linear, qualquer canto do painel pode ter dados; por isso os avisos vão
      // num subtítulo, em vez de uma caixa sobre o gráfico.
      var avisos = new List<string>();
      avisos.AddRange(GruposSobrepostos(seriesLog).Select(g => $"sobrepostos: {string.Join(" = ", g.Select(NomeCurto))}"));

      var proximosDeZero = maximosPorSerie
          .Where(par => par.Value < maximoY * 0.02)
          .Select(par => NomeCurto(par.Key))
          .ToList();
      if (proximosDeZero.Count > 0)
      {
        avisos.Add($"≈ 0 nesta escala: {string.Join(", ", proximosDeZero)}");
      }

      if (omitidas.Count > 0)
      {
        avisos.Add($"omitidos (quadráticos): {string.Join(", ", omitidas.Select(NomeCurto))}");
      }

      if (avisos.Count > 0)
      {
        grafico.Title($"{NomeDoCaso(caso)}\n{string.Join(" · ", avisos)}");
        grafico.Axes.Title.Label.FontSize = 15;
      }

      // Sem as omissões, todos os painéis têm as mesmas séries e uma legenda basta.
      // Com elas, cada painel tem séries diferentes e precisa da própria legenda.
      if (i == 0 || somenteSubquadraticos)
      {
        EstiloGraficos.MostrarLegenda(grafico, Alignment.UpperLeft);
      }
      else
      {
        grafico.HideLegend();
      }
    }

    int linhasDoGrid = (_casos.Count + colunas - 1) / colunas;
    multiplot.SavePng(caminhoPng, LarguraPainel * colunas, AlturaPainel * linhasDoGrid);
  }

  /// <summary>
  /// Quando duas ou mais séries têm praticamente os mesmos valores, as linhas ficam uma sobre
  /// a outra e só a última desenhada aparece. Descreve quais estão sobrepostas, para que
  /// nenhum algoritmo pareça ter "sumido". As séries devem estar em log₁₀.
  /// </summary>
  private static List<string> DescreverSobreposicoes(Dictionary<string, double[]> series)
  {
    var grupos = GruposSobrepostos(series);
    return grupos.Count == 0
        ? []
        : ["Linhas sobrepostas (mesmos valores):\n" + string.Join("\n", grupos.Select(g => string.Join(" = ", g)))];
  }

  /// <summary>Grupos de séries (em log₁₀) com praticamente os mesmos valores em todos os pontos.</summary>
  private static List<List<string>> GruposSobrepostos(Dictionary<string, double[]> series)
  {
    // Diferença máxima de 0,005 em log₁₀ (≈ 1%) em todos os pontos.
    const double Tolerancia = 0.005;

    var restantes = series.Keys.ToList();
    var grupos = new List<List<string>>();
    while (restantes.Count > 0)
    {
      string referencia = restantes[0];
      var grupo = restantes
          .Where(a => series[a].Length == series[referencia].Length
              && series[a].Zip(series[referencia]).All(p => Math.Abs(p.First - p.Second) < Tolerancia))
          .ToList();
      restantes.RemoveAll(grupo.Contains);
      if (grupo.Count > 1)
      {
        grupos.Add(grupo);
      }
    }

    return grupos;
  }

  /// <summary>Nome sem o sufixo " Sort", para caber nos subtítulos (ex.: "Merge").</summary>
  private static string NomeCurto(string algoritmo) =>
      algoritmo.EndsWith(" Sort", StringComparison.Ordinal) ? algoritmo[..^" Sort".Length] : algoritmo;

  /// <summary>Caixa de aviso no canto inferior direito do painel; não desenha nada se não houver avisos.</summary>
  private static void Anotar(Plot grafico, IReadOnlyList<string> avisos, double xDireita, double yInferior)
  {
    if (avisos.Count == 0)
    {
      return;
    }

    var anotacao = grafico.Add.Text(string.Join("\n\n", avisos), xDireita, yInferior);
    anotacao.Alignment = Alignment.LowerRight;
    anotacao.OffsetX = -10;
    anotacao.OffsetY = -10;
    anotacao.LabelFontSize = 13;
    anotacao.LabelFontColor = EstiloGraficos.TextoSecundario;
    anotacao.LabelBackgroundColor = EstiloGraficos.Fundo;
    anotacao.LabelBorderColor = EstiloGraficos.Eixo;
    anotacao.LabelBorderWidth = 1;
    anotacao.LabelPadding = 6;
  }

  /// <summary>Marcações do eixo x exatamente nos tamanhos medidos.</summary>
  private NumericManual MarcacoesNosTamanhos(bool log)
  {
    var marcacoes = new NumericManual();
    foreach (int n in _tamanhos)
    {
      marcacoes.AddMajor(log ? Math.Log10(n) : n, n.ToString("N0", EstiloGraficos.Cultura));
    }

    return marcacoes;
  }

  private List<LinhaResumo> Linhas(string algoritmo, CasoDeEntrada caso) =>
      [.. _resumo.Where(l => l.Algoritmo == algoritmo && l.Caso == caso).OrderBy(l => l.TamanhoAmostra)];

  public static string NomeDoCaso(CasoDeEntrada caso) => caso switch
  {
    CasoDeEntrada.Aleatorio => "Aleatório (caso médio)",
    CasoDeEntrada.Crescente => "Já ordenado (crescente)",
    CasoDeEntrada.Decrescente => "Ordem inversa (decrescente)",
    CasoDeEntrada.QuaseOrdenado => "Quase ordenado",
    CasoDeEntrada.OrdemOriginal => "Ordem original do arquivo",
    _ => caso.ToString(),
  };
}
