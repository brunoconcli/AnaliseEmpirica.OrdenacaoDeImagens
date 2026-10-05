namespace AnaliseEmpirica.OrdenacaoDeImagens.Analise;

/// <summary>Reta y = Coeficiente · n^Expoente ajustada aos pontos medidos.</summary>
/// <param name="R2">Coeficiente de determinação do ajuste em escala log-log (1 = reta perfeita).</param>
public sealed record AjustePotencia(double Expoente, double Coeficiente, double R2);

/// <summary>
/// Estima o expoente de crescimento de uma medida pela regressão linear de log(y) contra log(n).
/// </summary>
/// <remarks>
/// Se y ≈ c · n^k, então log y = log c + k · log n: uma reta cuja inclinação é k.
/// Para n² o expoente esperado é 2. Para n·log n, a inclinação local é 1 + 1/ln n,
/// ou seja, pouco acima de 1 (≈ 1,17 para n entre 100 e 1 000) e caindo devagar quando n cresce.
/// </remarks>
public static class AjusteDeCurva
{
  public static AjustePotencia AjustarPotencia(IReadOnlyList<double> tamanhos, IReadOnlyList<double> valores)
  {
    ArgumentNullException.ThrowIfNull(tamanhos);
    ArgumentNullException.ThrowIfNull(valores);
    if (tamanhos.Count != valores.Count || tamanhos.Count < 2)
    {
      throw new ArgumentException("São necessários ao menos 2 pares (n, valor) com o mesmo tamanho.");
    }

    if (tamanhos.Any(n => n <= 0) || valores.Any(v => v <= 0))
    {
      throw new ArgumentException("O ajuste em escala logarítmica exige valores positivos.");
    }

    double[] x = [.. tamanhos.Select(n => Math.Log(n))];
    double[] y = [.. valores.Select(v => Math.Log(v))];
    double mediaX = x.Average();
    double mediaY = y.Average();

    double covariancia = 0;
    double varianciaX = 0;
    for (int i = 0; i < x.Length; i++)
    {
      covariancia += (x[i] - mediaX) * (y[i] - mediaY);
      varianciaX += (x[i] - mediaX) * (x[i] - mediaX);
    }

    double expoente = covariancia / varianciaX;
    double intercepto = mediaY - expoente * mediaX;

    double residuos = 0;
    double total = 0;
    for (int i = 0; i < x.Length; i++)
    {
      double previsto = intercepto + expoente * x[i];
      residuos += (y[i] - previsto) * (y[i] - previsto);
      total += (y[i] - mediaY) * (y[i] - mediaY);
    }

    double r2 = total == 0 ? 1 : 1 - residuos / total;
    return new AjustePotencia(expoente, Math.Exp(intercepto), r2);
  }
}
