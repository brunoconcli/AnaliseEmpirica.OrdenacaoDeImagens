namespace AnaliseEmpirica.OrdenacaoDeImagens.Extracao;

/// <summary>
/// Parâmetros da extração. Devem ficar fixos durante todo o experimento e ser
/// informados no relatório; os limiares ainda precisam ser calibrados com o dataset.
/// </summary>
/// <param name="LadoRedimensionamento">Lado, em pixels, da imagem quadrada após o redimensionamento.</param>
/// <param name="LimiarCromaAcromatica">τ: abaixo desta croma média a imagem é acromática.</param>
/// <param name="LimiarConcentracaoMatiz">
/// ρ: abaixo desta concentração dos matizes (comprimento do vetor médio ÷ peso total)
/// as cores estão espalhadas pelo círculo e a imagem também é tratada como acromática.
/// </param>
/// <param name="LimiarBorda">T: magnitude mínima do gradiente de Sobel para um pixel contar como borda.</param>
public sealed record ParametrosExtracao(
    int LadoRedimensionamento = 128,
    double LimiarCromaAcromatica = 0.05,
    double LimiarConcentracaoMatiz = 0.1,
    double LimiarBorda = 100)
{
  public static ParametrosExtracao Padrao { get; } = new();
}
