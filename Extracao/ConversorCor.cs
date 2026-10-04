namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

/// <summary>
/// Conversões de um pixel RGB (0–255 por canal) para as grandezas usadas pelos extratores.
/// </summary>
public static class ConversorCor
{
  /// <summary>
  /// Luma pelos coeficientes da ITU-R BT.601, em [0, 255].
  /// É luma (Y′), e não luminância, porque os canais já vêm com correção gama (sRGB).
  /// </summary>
  public static double Luma(byte r, byte g, byte b) => 0.299 * r + 0.587 * g + 0.114 * b;

  /// <summary>
  /// Croma normalizada: (max − min) / 255, em [0, 1].
  /// Equivale à saturação HSV ponderada pelo brilho (S·V).
  /// </summary>
  public static double Croma(byte r, byte g, byte b) =>
      (Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b))) / 255.0;

  /// <summary>
  /// Matiz HSV em graus, em [0, 360). Para pixels sem croma (cinza) o matiz
  /// é indefinido e o valor retornado é 0; os extratores devem ponderar o
  /// matiz pela croma para que esses pixels não influenciem o resultado.
  /// </summary>
  public static double Matiz(byte r, byte g, byte b)
  {
    int maximo = Math.Max(r, Math.Max(g, b));
    int minimo = Math.Min(r, Math.Min(g, b));
    int croma = maximo - minimo;

    if (croma == 0)
    {
      return 0;
    }

    double setor;
    if (maximo == r)
    {
      setor = (double)(g - b) / croma;
    }
    else if (maximo == g)
    {
      setor = (double)(b - r) / croma + 2;
    }
    else
    {
      setor = (double)(r - g) / croma + 4;
    }

    double graus = setor * 60;
    return graus < 0 ? graus + 360 : graus;
  }
}
