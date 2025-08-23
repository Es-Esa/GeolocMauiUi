using System.Collections.ObjectModel;
using ClientApp.Core.Data;
using ClientApp.Core.Domain;

namespace ClientApp.Core.ViewModels;

public class MapPageViewModel : BaseViewModel
{
    private readonly ISightingRepository _sightingRepository;

    public ObservableCollection<Sighting> Sightings { get; } = new();

    public MapPageViewModel(ISightingRepository sightingRepository)
    {
        _sightingRepository = sightingRepository;
        Title = "Map";
    }

    public async Task InitializeAsync()
    {
        Sightings.Clear();
        var sightings = await _sightingRepository.GetAllSightingsAsync();
        foreach (var sighting in sightings)
        {
            Sightings.Add(sighting);
        }
    }
}
