namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Instrumentacao;

/// <summary>
/// Selection Sort clássico: a cada passo, seleciona o menor elemento restante
/// e o troca com a primeira posição não ordenada.
/// </summary>
/// <remarks>
/// Comparações: exatamente n(n − 1)/2 em qualquer caso, O(n²).
/// Movimentações: exatamente 2(n − 1), pois realiza uma troca por passo
/// (inclusive quando o mínimo já está na posição correta).
/// Não estável: a troca pode passar um elemento por cima de outro equivalente.
/// </remarks>
public sealed class SelectionSort : IAlgoritmoOrdenacao
{
    public string Nome => "Selection Sort";
    public bool Estavel => false;

    public void Ordenar<T>(T[] vetor, IComparer<T> comparador, Contadores contadores)
    {
        ArgumentNullException.ThrowIfNull(vetor);
        ArgumentNullException.ThrowIfNull(comparador);
        ArgumentNullException.ThrowIfNull(contadores);

        for (int i = 0; i < vetor.Length - 1; i++)
        {
            int indiceMinimo = i;

            for (int j = i + 1; j < vetor.Length; j++)
            {
                if (comparador.Compare(vetor[j], vetor[indiceMinimo]) < 0)
                {
                    indiceMinimo = j;
                }
            }

            OperacoesVetor.Trocar(vetor, i, indiceMinimo, contadores);
        }
    }
}
