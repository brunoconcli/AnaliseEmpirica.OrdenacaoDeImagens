namespace AnaliseEmpirica.OrdenacaoDeImagens.Algoritmos;

using AnaliseEmpirica.OrdenacaoDeImagens.Models;

public interface IAlgoritmoOrdenacao
{
  string Nome { get; }
  MetricasDeOrdenacao AplicarOrdenacao(ItemImagem[] entrada);
}