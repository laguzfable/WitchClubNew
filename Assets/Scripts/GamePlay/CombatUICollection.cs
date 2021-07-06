using UnityEngine;

public class CombatUICollection : MonoBehaviour
{
    public GameObject Runes;
    public GameObject cards;
    public GameObject turnBtn;
    public GameObject playerStat;
    public GameObject playerHP;
    public GameObject mobHP;
    public GameObject mobStat;

    public BossUnit mob;

    public GameObject env;

    public void SetRunesEnabled(bool enable)
    {
        Runes.SetActive(enable);
    }

    public void TurnOffAll()
    {
        Runes.SetActive(false);
        cards.SetActive(false);
        turnBtn.SetActive(false);
        playerStat.SetActive(false);
        playerHP.SetActive(false);
        mobHP.SetActive(false);
        mobStat.SetActive(false);
        mob.sprRend.enabled = false;
        env.SetActive(false);
    }
}