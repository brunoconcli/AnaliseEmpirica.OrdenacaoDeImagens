namespace AnaliseEmpirica.OrdenacaoDeImagens.Modelos;

/// <summary>
/// Propriedades visuais extraídas de cada imagem (ver docs/estudo-propps.md).
/// </summary>
public enum Propriedade
{
    /// <summary>A. Luma média (BT.601), em [0, 255].</summary>
    Luminosidade,

    /// <summary>B. Média circular do matiz ponderada pela croma, em [0, 360) ou −1 se acromática.</summary>
    Tonalidade,

    /// <summary>C. Croma média, em [0, 1].</summary>
    Saturacao,

    /// <summary>D. Densidade de bordas (Sobel), em [0, 1].</summary>
    Complexidade,

    /// <summary>Métrica alternativa: entropia de Shannon do histograma (diversidade tonal), em [0, 8] bits.</summary>
    Entropia
}
