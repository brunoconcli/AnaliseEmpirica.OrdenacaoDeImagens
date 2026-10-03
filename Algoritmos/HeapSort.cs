namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Heap Sort com heap máximo: constrói o heap de baixo para cima e, em seguida,
/// move repetidamente o maior elemento (raiz) para o fim da parte não ordenada.
/// </summary>
/// <remarks>
/// Construção do heap: O(n). Extrações: n − 1 descidas de custo O(log n).
/// Comparações: ~2n·log₂n em qualquer caso, O(n log n).
/// Memória extra: O(1).
/// Não estável: a troca da raiz com o último elemento desfaz a ordem entre equivalentes.
/// </remarks>
public sealed class HeapSort : IAlgoritmoOrdenacao
{
    public string Nome => "Heap Sort";
    public bool Estavel => false;

    public void Ordenar<T>(T[] vetor, IComparer<T> comparador, Contadores contadores)
    {
        ArgumentNullException.ThrowIfNull(vetor);
        ArgumentNullException.ThrowIfNull(comparador);
        ArgumentNullException.ThrowIfNull(contadores);

        int tamanho = vetor.Length;

        // Construção do heap: desce cada nó interno, do último até a raiz.
        for (int i = tamanho / 2 - 1; i >= 0; i--)
        {
            Descer(vetor, i, tamanho, comparador, contadores);
        }

        // Extração: a raiz (maior elemento) vai para o fim da região do heap.
        for (int fim = tamanho - 1; fim > 0; fim--)
        {
            OperacoesVetor.Trocar(vetor, 0, fim, contadores);
            Descer(vetor, 0, fim, comparador, contadores);
        }
    }

    /// <summary>
    /// Restaura a propriedade de heap máximo a partir de <paramref name="no"/>,
    /// considerando apenas as posições [0, <paramref name="tamanhoHeap"/>).
    /// </summary>
    private static void Descer<T>(T[] vetor, int no, int tamanhoHeap, IComparer<T> comparador, Contadores contadores)
    {
        while (true)
        {
            int esquerdo = 2 * no + 1;
            int direito = esquerdo + 1;
            int maior = no;

            if (esquerdo < tamanhoHeap && comparador.Compare(vetor[esquerdo], vetor[maior]) > 0)
            {
                maior = esquerdo;
            }

            if (direito < tamanhoHeap && comparador.Compare(vetor[direito], vetor[maior]) > 0)
            {
                maior = direito;
            }

            if (maior == no)
            {
                return;
            }

            OperacoesVetor.Trocar(vetor, no, maior, contadores);
            no = maior;
        }
    }
}
