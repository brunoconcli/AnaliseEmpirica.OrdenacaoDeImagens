namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Quick Sort clássico com partição de Lomuto e o último elemento como pivô.
/// </summary>
/// <remarks>
/// Melhor caso e caso médio: O(n log n), ~1,39·n·log₂n comparações no caso médio.
/// Pior caso: n(n − 1)/2 comparações, O(n²). Ocorre quando o pivô é sempre o
/// menor ou o maior elemento: vetor já ordenado, em ordem inversa, ou com
/// muitas chaves repetidas (ex.: Hue quantizado em poucas faixas).
/// Não estável: a partição troca elementos distantes entre si.
///
/// A recursão é feita apenas sobre a menor partição; a maior é tratada no laço.
/// Isso limita a profundidade da pilha a O(log n) mesmo no pior caso, evitando
/// estouro de pilha com n grande, sem alterar comparações nem movimentações.
/// </remarks>
public sealed class QuickSort : IAlgoritmoOrdenacao
{
    public string Nome => "Quick Sort";
    public bool Estavel => false;

    public void Ordenar<T>(T[] vetor, IComparer<T> comparador, Contadores contadores)
    {
        ArgumentNullException.ThrowIfNull(vetor);
        ArgumentNullException.ThrowIfNull(comparador);
        ArgumentNullException.ThrowIfNull(contadores);

        OrdenarIntervalo(vetor, 0, vetor.Length - 1, comparador, contadores);
    }

    private static void OrdenarIntervalo<T>(T[] vetor, int inicio, int fim, IComparer<T> comparador, Contadores contadores)
    {
        while (inicio < fim)
        {
            int posicaoPivo = Particionar(vetor, inicio, fim, comparador, contadores);

            if (posicaoPivo - inicio < fim - posicaoPivo)
            {
                OrdenarIntervalo(vetor, inicio, posicaoPivo - 1, comparador, contadores);
                inicio = posicaoPivo + 1;
            }
            else
            {
                OrdenarIntervalo(vetor, posicaoPivo + 1, fim, comparador, contadores);
                fim = posicaoPivo - 1;
            }
        }
    }

    /// <summary>
    /// Partição de Lomuto: usa vetor[fim] como pivô e deixa à sua esquerda os
    /// elementos menores ou iguais a ele. Retorna a posição final do pivô.
    /// </summary>
    private static int Particionar<T>(T[] vetor, int inicio, int fim, IComparer<T> comparador, Contadores contadores)
    {
        T pivo = vetor[fim];
        int i = inicio - 1;

        for (int j = inicio; j < fim; j++)
        {
            if (comparador.Compare(vetor[j], pivo) <= 0)
            {
                i++;
                OperacoesVetor.Trocar(vetor, i, j, contadores);
            }
        }

        OperacoesVetor.Trocar(vetor, i + 1, fim, contadores);
        return i + 1;
    }
}
