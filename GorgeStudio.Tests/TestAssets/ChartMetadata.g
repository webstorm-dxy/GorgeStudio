[
    string form = "Dremu",
    string displayName = "Dremu谱表"
]
@ElementStaff
class DremuStaff
{
    int Health;
    float Speed;

    @Chart
    static int Period()
    {
        return 42;
    }

    void Update() {}
}
