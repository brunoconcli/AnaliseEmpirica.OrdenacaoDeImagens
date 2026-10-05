namespace AnaliseEmpirica.OrdenacaoDeImagens.Analise;

using System.Globalization;

using ScottPlot;

/// <summary>
/// Cores, formas e formatação comuns a todos os gráficos.
/// </summary>
/// <remarks>
/// Paleta categórica validada para 5 séries (daltonismo e visão normal) com o validador
/// do guia de visualização. Três cores têm contraste baixo com o fundo branco; por isso,
/// cada algoritmo também tem um formato de marcador próprio, há sempre legenda, e o
/// resumo.csv serve de versão em tabela de cada gráfico.
/// A cor acompanha o algoritmo (nunca a posição dele no ranking), em todos os gráficos.
/// </remarks>
public static class EstiloGraficos
{
  public static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("pt-BR");

  public static readonly Color Fundo = Color.FromHex("#ffffff");
  public static readonly Color TextoPrincipal = Color.FromHex("#0b0b0b");
  public static readonly Color TextoSecundario = Color.FromHex("#52514e");
  public static readonly Color LinhaDeGrade = Color.FromHex("#e1e0d9");
  public static readonly Color Eixo = Color.FromHex("#c3c2b7");
  public static readonly Color Teoria = Color.FromHex("#898781");

  /// <summary>Rampa sequencial azul (claro → escuro) para valores, como no mapa de calor.</summary>
  public static readonly Color[] RampaSequencial =
  [
    Color.FromHex("#cde2fb"), Color.FromHex("#b7d3f6"), Color.FromHex("#9ec5f4"), Color.FromHex("#86b6ef"),
    Color.FromHex("#6da7ec"), Color.FromHex("#5598e7"), Color.FromHex("#3987e5"), Color.FromHex("#2a78d6"),
    Color.FromHex("#256abf"), Color.FromHex("#1c5cab"), Color.FromHex("#184f95"), Color.FromHex("#104281"),
    Color.FromHex("#0d366b"),
  ];

  private static readonly Dictionary<string, (Color Cor, MarkerShape Marcador)> _porAlgoritmo = new()
  {
    ["Insertion Sort"] = (Color.FromHex("#2a78d6"), MarkerShape.FilledCircle),
    ["Selection Sort"] = (Color.FromHex("#eb6834"), MarkerShape.FilledSquare),
    ["Merge Sort"] = (Color.FromHex("#1baf7a"), MarkerShape.FilledTriangleUp),
    ["Heap Sort"] = (Color.FromHex("#eda100"), MarkerShape.FilledDiamond),
    ["Quick Sort"] = (Color.FromHex("#e87ba4"), MarkerShape.FilledTriangleDown),
  };

  public static (Color Cor, MarkerShape Marcador) DoAlgoritmo(string algoritmo) =>
      _porAlgoritmo.TryGetValue(algoritmo, out var estilo) ? estilo : (TextoSecundario, MarkerShape.OpenCircle);

  /// <summary>Fundo, grade discreta, eixos e textos em tons neutros.</summary>
  public static void AplicarBase(Plot grafico, string titulo, string rotuloX, string rotuloY)
  {
    grafico.FigureBackground.Color = Fundo;
    grafico.DataBackground.Color = Fundo;
    grafico.Axes.Color(TextoSecundario);
    grafico.Axes.FrameColor(Eixo);
    grafico.Grid.MajorLineColor = LinhaDeGrade;
    grafico.Grid.MajorLineWidth = 1;

    grafico.Title(titulo);
    grafico.Axes.Title.Label.ForeColor = TextoPrincipal;
    grafico.Axes.Title.Label.FontSize = 18;
    grafico.XLabel(rotuloX);
    grafico.YLabel(rotuloY);
    grafico.Axes.Bottom.Label.FontSize = 14;
    grafico.Axes.Left.Label.FontSize = 14;
  }

  public static void MostrarLegenda(Plot grafico, Alignment posicao)
  {
    grafico.ShowLegend(posicao);
    grafico.Legend.FontSize = 13;
    grafico.Legend.FontColor = TextoPrincipal;
    grafico.Legend.BackgroundColor = Fundo;
    grafico.Legend.OutlineColor = Eixo;
    grafico.Legend.OutlineWidth = 1;
    grafico.Legend.ShadowColor = Colors.Transparent;
  }

  /// <summary>Formata 10^expoente para os rótulos de um eixo logarítmico.</summary>
  public static string FormatarPotenciaDeDez(double expoente)
  {
    double valor = Math.Pow(10, expoente);
    return valor >= 1 ? valor.ToString("N0", Cultura) : valor.ToString("0.####", Cultura);
  }
}
