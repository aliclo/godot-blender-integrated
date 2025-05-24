using Godot;

public interface IStorable {

    public Variant GetData();

    public void Load(Variant data);

}