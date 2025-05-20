using UnityEngine;
using UnityEngine.UI;

public class BookMenu : MonoBehaviour
{
    public Book book;
    public GameObject[] FirstPage;
    public GameObject[] SecondPage;
    public GameObject[] ThirdPage;

    public bool isfirst;
    public bool isSecond;
    public bool isThird;
    public bool isNone;
    public bool hasSwitched;
    public bool IsFlipping;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        handlePage();
        if (!IsFlipping)
        {

            if (isfirst && hasSwitched)
            {
                Debug.Log("isFirst");
                foreach (GameObject x in FirstPage)
                {
                    x.SetActive(true);
                }
                foreach (GameObject x in SecondPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in ThirdPage)
                {
                    x.SetActive(false);
                }
                hasSwitched = false;
            }
            else if (isSecond && hasSwitched)
            {
                Debug.Log("isSecond");
                foreach (GameObject x in FirstPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in SecondPage)
                {
                    x.SetActive(true);
                }
                foreach (GameObject x in ThirdPage)
                {
                    x.SetActive(false);
                }
                hasSwitched = false;
            }
            else if (isThird && hasSwitched)
            {
                Debug.Log("isThird");
                foreach (GameObject x in FirstPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in SecondPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in ThirdPage)
                {
                    x.SetActive(true);
                }
                hasSwitched = false;
            }
            else if (isNone && hasSwitched)
            {
                Debug.Log("isNone");
                foreach (GameObject x in FirstPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in SecondPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in ThirdPage)
                {
                    x.SetActive(false);
                }
                hasSwitched = false;
            }
        }
        else
        {
            Debug.Log("isNone");
                foreach (GameObject x in FirstPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in SecondPage)
                {
                    x.SetActive(false);
                }
                foreach (GameObject x in ThirdPage)
                {
                    x.SetActive(false);
                }
                hasSwitched = false;
        }
    }

    void handlePage()
    {
        switch (book.currentPage)
        {
            case (0):
                isfirst = true;
                isSecond = false;
                isNone = false;
                hasSwitched = true;
                break;

            case (2):
                isSecond = true;
                isfirst = false;
                isNone = false;
                hasSwitched = true;
                break;

            case (4):
                isSecond = false;
                isfirst = false;
                isThird = true;
                isNone = false;
                hasSwitched = true;
                break;

            default:
                isSecond = false;
                isfirst = false;
                isNone = true;
                hasSwitched = true;
                break;
        }
    }
    public void StartFlip()
    {
        Debug.Log("flipping");
        IsFlipping = true;
    }
    public void EndFlip()
    {
        Debug.Log("flipping");
        IsFlipping = false;
    }
}
