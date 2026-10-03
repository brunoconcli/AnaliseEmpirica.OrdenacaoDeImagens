namespace AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Uma linha dos resultados brutos: uma execução de um algoritmo sobre uma amostra.
/// </summary>
/// <param name="Algoritmo">Nome do algoritmo de ordenação.</param>
/// <param name="Criterio">Descrição do comparador usado (ex.: "Luminosidade crescente").</param>
/// <param name="Caso">Arranjo inicial da amostra.</param>
/// <param name="TamanhoAmostra">Número de elementos ordenados (n).</param>
/// <param name="Repeticao">Índice da repetição dentro do mesmo grupo (1, 2, ...).</param>
/// <param name="Semente">Semente usada para sortear a amostra, para reprodutibilidade.</param>
/// <param name="TempoMs">Tempo de ordenação em milissegundos, medido sem instrumentação de comparações.</param>
/// <param name="Comparacoes">Número de comparações entre elementos.</param>
/// <param name="Movimentacoes">Número de escritas de elementos em vetores.</param>
public sealed record ResultadoExecucao(
    string Algoritmo,
    string Criterio,
    CasoDeEntrada Caso,
    int TamanhoAmostra,
    int Repeticao,
    int Semente,
    double TempoMs,
    long Comparacoes,
    long Movimentacoes);
