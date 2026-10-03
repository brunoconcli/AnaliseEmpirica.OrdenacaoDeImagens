namespace AnaliseEmpirica.OrdenacaoDeImagens.Models;

public enum TipoMetrica
{
    Luminosidade,
    Tonalidade,
    Saturacao,
    Entropia
}

public class ItemImagem
{
    public string CaminhoArquivo { get; set; } = string.Empty;

    public double Luminosidade { get; set; }
    public double Tonalidade { get; set; }
    public double Saturacao { get; set; }
    public double Entropia { get; set; }

    public double ValorChave { get; private set; }

    public void DefinirChaveAtiva(TipoMetrica metrica)
    {
        ValorChave = metrica switch
        {
            TipoMetrica.Luminosidade => Luminosidade,
            TipoMetrica.Tonalidade => Tonalidade,
            TipoMetrica.Saturacao => Saturacao,
            TipoMetrica.Entropia => Entropia,
            _ => Luminosidade
        };
    }
}