namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Insertion Sort clássico, com deslocamento dos elementos maiores para a direita.
/// </summary>
/// <remarks>
/// Melhor caso (vetor ordenado): n − 1 comparações, O(n).
/// Pior caso (vetor em ordem inversa): n(n − 1)/2 comparações, O(n²).
/// Caso médio: ~n²/4 comparações, O(n²).
/// Estável: só desloca elementos estritamente maiores que a chave.
/// </remarks>
public sealed class InsertionSort : IAlgoritmoOrdenacao
{
    public string Nome => "Insertion Sort";
    public bool Estavel => true;

    public void Ordenar<T>(T[] vetor, IComparer<T> comparador, Contadores contadores)
    {
        ArgumentNullException.ThrowIfNull(vetor);
        ArgumentNullException.ThrowIfNull(comparador);
        ArgumentNullException.ThrowIfNull(contadores);

        for (int i = 1; i < vetor.Length; i++)
        {
            T chave = vetor[i];
            int j = i - 1;

            while (j >= 0 && comparador.Compare(vetor[j], chave) > 0)
            {
                vetor[j + 1] = vetor[j];
                contadores.RegistrarMovimentacao();
                j--;
            }

            vetor[j + 1] = chave;
            contadores.RegistrarMovimentacao();
        }
    }
}
