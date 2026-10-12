#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace HobbyDS9.UnityClient
{
    // Unity-client-local types, NOT shared portable gameplay contracts. A future
    // Application adapter translates these IDs and consumes backend-owned results.
    public enum DesignerProfile { Cruise, Combat, Survey }
    public enum DesignerStatus { Valid, Warning, HardError, NotEvaluated }
    public sealed class DesignerHull
    {
        public string Id { get; }
        public string Caption { get; }
        public string Facts { get; }
        public DesignerHull(string id,string caption,string facts) { Id=id; Caption=caption; Facts=facts; }
    }
    public sealed class DesignerComponent
    {
        public string Id { get; }
        public string Name { get; }
        public string Group { get; }
        public string Description { get; }
        public string Facts { get; }
        public DesignerComponent(string id, string name, string group, string description, string facts)
        { Id = id; Name = name; Group = group; Description = description; Facts = facts; }
    }
    public sealed class DesignerFit
    {
        public string Name { get; }
        public string HullId { get; }
        public string Role { get; }
        public IReadOnlyDictionary<string,int> Installed { get; }
        public DesignerFit(string name, string hullId, string role, IEnumerable<KeyValuePair<string,int>> installed)
        {
            Name = name; HullId = hullId; Role = role;
            Installed = new System.Collections.ObjectModel.ReadOnlyDictionary<string,int>(installed.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal));
        }
        public bool SameAs(DesignerFit other) => Name == other.Name && HullId == other.HullId && Role == other.Role && SameEquipment(other);
        public bool SameEquipment(DesignerFit other) => Installed.Count == other.Installed.Count
            && Installed.All(p => other.Installed.TryGetValue(p.Key, out var count) && count == p.Value);
    }
    public sealed class DesignerTemplate
    {
        public string Id { get; }
        public string Caption { get; }
        public DesignerFit Fit { get; }
        public DesignerTemplate(string id, string caption, DesignerFit fit) { Id = id; Caption = caption; Fit = fit; }
    }
    public sealed class DesignerIssue
    {
        public string SystemId { get; }
        public DesignerStatus Severity { get; }
        public string Explanation { get; }
        public DesignerIssue(string systemId, DesignerStatus severity, string explanation)
        { SystemId = systemId; Severity = severity; Explanation = explanation; }
    }
    public sealed class DesignerMetric
    {
        public string Caption { get; }
        public string Value { get; }
        public DesignerMetric(string caption, string value) { Caption = caption; Value = value; }
    }
    public sealed class DesignerPreview
    {
        public DesignerStatus Status { get; }
        public string Notice { get; }
        public int? UsedCapacity { get; }
        public int? TotalCapacity { get; }
        public int? FreeCapacity { get; }
        public IReadOnlyList<DesignerMetric> Power { get; }
        public IReadOnlyList<DesignerMetric> Capabilities { get; }
        public IReadOnlyList<DesignerMetric> Costs { get; }
        public IReadOnlyList<DesignerIssue> Issues { get; }
        public DesignerPreview(DesignerStatus status, string notice, int? used = null, int? total = null, int? free = null,
            IEnumerable<DesignerMetric>? power = null, IEnumerable<DesignerMetric>? capabilities = null,
            IEnumerable<DesignerMetric>? costs = null, IEnumerable<DesignerIssue>? issues = null)
        {
            Status = status; Notice = notice; UsedCapacity = used; TotalCapacity = total; FreeCapacity = free;
            Power = (power ?? Array.Empty<DesignerMetric>()).ToList().AsReadOnly();
            Capabilities = (capabilities ?? Array.Empty<DesignerMetric>()).ToList().AsReadOnly();
            Costs = (costs ?? Array.Empty<DesignerMetric>()).ToList().AsReadOnly();
            Issues = (issues ?? Array.Empty<DesignerIssue>()).ToList().AsReadOnly();
        }
    }
    public interface IShipDesignPreviewProvider
    {
        string StatusLabel { get; }
        bool FixtureResults { get; }
        IReadOnlyList<DesignerHull> Hulls { get; }
        IReadOnlyList<DesignerComponent> Catalogue { get; }
        IReadOnlyList<DesignerTemplate> Templates { get; }
        DesignerPreview Preview(DesignerFit fit, DesignerProfile profile);
    }

    // Only local editing and lifecycle. No simulation reference, commands, state,
    // capacity validation, power allocation or capability/cost formulas.
    public sealed class ShipDesignDraft
    {
        public IShipDesignPreviewProvider Provider { get; }
        public DesignerFit Starting { get; private set; } = null!;
        public string TemplateId { get; private set; } = "";
        public DesignerProfile Profile { get; private set; } = DesignerProfile.Cruise;
        public DesignerFit Current => new DesignerFit(name, hullId, role, installed);
        public bool Modified => !Current.SameAs(Starting);
        public bool ConfirmationPending => pending != null;
        public event Action? Changed;
        private string name = "", role = "", hullId = "";
        private readonly Dictionary<string,int> installed = new Dictionary<string,int>(StringComparer.Ordinal);
        private Action? pending;
        public ShipDesignDraft(IShipDesignPreviewProvider provider)
        { Provider = provider; StartTemplate(provider.Templates.First().Id); }
        private void Notify() => Changed?.Invoke();
        private void CopyStarting()
        {
            name = Starting.Name; role = Starting.Role; hullId = Starting.HullId; installed.Clear();
            foreach (var pair in Starting.Installed) installed.Add(pair.Key, pair.Value);
        }
        private void StartTemplate(string id)
        {
            var template = Provider.Templates.Single(t => t.Id == id);
            TemplateId = id; Starting = new DesignerFit(template.Fit.Name, template.Fit.HullId, template.Fit.Role, template.Fit.Installed);
            CopyStarting(); Profile = DesignerProfile.Cruise; pending = null; Notify();
        }
        public void Rename(string value) { if (name == value) return; name = value; Notify(); }
        public void SetHull(string value)
        {
            if(!Provider.Hulls.Any(h=>h.Id==value)) throw new ArgumentException("Unknown hull version.",nameof(value));
            if(hullId==value) return; hullId=value; Notify();
        }
        public void SetRole(string value) { if (role == value) return; role = value; Notify(); }
        public void SelectProfile(DesignerProfile value) { Profile = value; Notify(); }
        public void SetQuantity(string componentId, int quantity)
        {
            if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity)); // Local edit shape, not legality.
            if (!Provider.Catalogue.Any(c => c.Id == componentId)) throw new ArgumentException("Unknown catalogue item.", nameof(componentId));
            if (quantity == 0) installed.Remove(componentId); else installed[componentId] = quantity;
            Notify();
        }
        public void Add(string id) => SetQuantity(id, checked((installed.TryGetValue(id,out var count) ? count : 0) + 1));
        public void Remove(string id) => SetQuantity(id, 0);
        public void Replace(string oldId, string replacementId)
        {
            if (oldId == replacementId) return;
            if (!installed.TryGetValue(oldId,out var amount)) throw new ArgumentException("Select an installed item first.", nameof(oldId));
            if (!Provider.Catalogue.Any(c => c.Id == replacementId)) throw new ArgumentException("Unknown replacement.", nameof(replacementId));
            var quantity = checked(amount + (installed.TryGetValue(replacementId,out var count) ? count : 0));
            installed.Remove(oldId); installed[replacementId] = quantity; Notify();
        }
        public void RequestTemplate(string id)
        {
            if (!Provider.Templates.Any(t => t.Id == id)) throw new ArgumentException("Unknown template.", nameof(id));
            RequestDiscard(() => StartTemplate(id));
        }
        public void RequestReset() => RequestDiscard(() => { CopyStarting(); Notify(); });
        public void RequestDiscard(Action afterDiscard)
        {
            if (Modified) { pending = afterDiscard; Notify(); }
            else afterDiscard();
        }
        public void CancelDiscard() { pending = null; Notify(); }
        public void ConfirmDiscard()
        {
            var action = pending; if (action == null) return;
            pending = null; CopyStarting(); action(); Notify();
        }
        public DesignerPreview Preview(bool starting = false)
        {
            try { return Provider.Preview(starting ? Starting : Current, Profile); }
            catch { return new DesignerPreview(DesignerStatus.NotEvaluated, "Preview provider unavailable. Your local draft is retained; no campaign order was sent."); }
        }
    }
}
