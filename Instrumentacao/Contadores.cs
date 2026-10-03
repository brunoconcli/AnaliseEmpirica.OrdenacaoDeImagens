namespace AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Acumula as medidas de custo de uma execução de ordenação.
/// </summary>
/// <remarks>
/// Regras de contagem (iguais para todos os algoritmos):
/// <list type="bullet">
///   <item><b>Comparação:</b> cada chamada ao comparador entre dois elementos.
///   É registrada pelo <see cref="ComparadorContador{T}"/>, nunca pelo algoritmo.</item>
///   <item><b>Movimentação:</b> cada escrita de um elemento em uma posição de vetor,
///   seja o vetor de entrada ou um vetor auxiliar. Cópias para variáveis locais
///   (ex.: a "chave" do Insertion Sort) não contam. Uma troca equivale a 2 movimentações.</item>
/// </list>
/// "Movimentações" é usado em vez de "trocas" porque o Merge Sort e o Insertion Sort
/// não trocam elementos, apenas os copiam ou deslocam.
/// </remarks>
public sealed class Contadores
{
    public long Comparacoes { get; private set; }
    public long Movimentacoes { get; private set; }

    public void RegistrarComparacao() => Comparacoes++;

    public void RegistrarMovimentacao() => Movimentacoes++;

    public void RegistrarMovimentacoes(long quantidade) => Movimentacoes += quantidade;

    public void Zerar()
    {
        Comparacoes = 0;
        Movimentacoes = 0;
    }

    public override string ToString() =>
        $"Comparações: {Comparacoes:N0} | Movimentações: {Movimentacoes:N0}";
}
