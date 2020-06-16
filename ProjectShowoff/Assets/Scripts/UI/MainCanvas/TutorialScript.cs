﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Image = UnityEngine.UI.Image;
using Slider = UnityEngine.UI.Slider;

public class TutorialScript : MonoBehaviour
{
    //drag all UI elements in + anything else we might need to alter or turn off
    public GameObject zoomSlider;
    public GameObject announcerBox;
    public GameObject timeImages;
    public GameObject reset;
    public GameObject tutorialArrow;
    public GameObject tutorialHand;
    public TMP_Text questBoxTextTMP;
    public GameObject questSystem;
    public GameObject ruralBuildings;
    public Image questBoxImage;
    private Slider slider;
    private float animationSpeed = 1; //reset at end of every animation, makes animations move faster as they go on
    //tutorial step bools so we can check where the player is
    private bool tutorialRotationStep;
    private bool tutorialZoomStep;
    private bool tutorialBuildStep;
    private bool gameHasStarted;
    
    private float tutorialDelaySeconds = 2.5f;//how long you need to wait before you start a tutorial step, potentially redundant
    public bool tutorialSkip;//if active, start does all the tutorial steps right away
    private bool English;//if true, text in english, if false, text is in dutch
    private int buildingCount;//for the tutorial steps, how many buildings have you placed yet?
    private float timer;
    
    void Start()
    {
        BuildingSystem.onBuildingPlaced += OnBuildingPlaced;
        slider = zoomSlider.GetComponent<Slider>();
        slider.onValueChanged.AddListener(SliderTutorialChange);
        GameManager.paused = true;
        StartCoroutine(NewsCasterAnimationStart());
        StartCoroutine(HandAnimation());
        if (tutorialSkip)
        {
            //basically sets the delay of the animations to 0 nd just throws em all in at the start
            tutorialDelaySeconds = 0;
            tutorialBuildStep = true;
            tutorialZoomStep = true;
            tutorialRotationStep = true;
            GameManager.paused = false;
            StartCoroutine(TimerAnimationStart());
            StartCoroutine(ResetAnimationStart());
            StartCoroutine(SliderAnimationStart());
            StartCoroutine(HandAnimation());
        }
    }
    //this function gets called when ui slider disabled the main canvas, its the first thing that gets called
    public void OnEnable()
    {
        if (LanguageSelector.LanguageSelected == LanguageSelector.LanguageSelectorSelected.English)
        {
            English = true;
        }
        tutorialArrow.SetActive(false);
        tutorialHand.SetActive(false);
        tutorialRotationStep = true;
        foreach (Transform child in ruralBuildings.transform)
        {
            if (child.gameObject.name != "Factory")
            {
                child.gameObject.SetActive(false);
            }
        }

        if (English)
        {
            questBoxTextTMP.text = "Click on the purple dome and place a factory there!";
        } else
        {
            questBoxTextTMP.text = "Klik op de paarse cirkel en plaats daar een fabriek!";
        }

        //StartCoroutine(QuestBoxFlash());
        //basically set all the stuff that needs to be set, set the starting text, etc
    }
    //this gets called when a building is placed, we have tutorialized the first 4 buildings so far 
    private void OnBuildingPlaced(BuildingLocation location, BuildingPlacer buildingData, Building building)
    {
        buildingCount += 1;
        if (buildingCount == 1)
        {
            StartCoroutine(BuildingNatureReserveWaiter("Nature reserve"));
            if (English)
            {
                questBoxTextTMP.text = "Good job! Now place a nature reserve to balance the pollution!";
            }
            else
            {
                questBoxTextTMP.text = "Goed gedaan! Plaats nu een natuurgebied om de vervuiling te stoppen!";
            }   
        }
        else if (buildingCount == 2)
        {
            StartCoroutine(BuildingActivationWaiter());
            if (English)
            {
                questBoxTextTMP.text = "Nice! There is one spot left, why don't you try something new?";
            }
            else
            {
                questBoxTextTMP.text = "Netjes! Er is nog een plek over, waarom probeer je niet iets nieuws?";
            }  
        }
        else if (buildingCount == 3)
        {
            if (English)
            {
                questBoxTextTMP.text = "Wow! The island is full! Try zooming out and finding a new spot!";
            }
            else
            {
                questBoxTextTMP.text = "Wow! Het hele eiland is vol! Probeer eens uit te zoomen en een nieuwe plek te vinden!";
            }
            StartCoroutine(SliderAnimationStart());
        }
        else if (buildingCount == 4)
        {
            if (English)
            {
                questBoxTextTMP.text = "Good job, you can place what you want now, but you can help us more!.";
            }
            else
            {
                questBoxTextTMP.text = "Goed gedaan, je kan nu doen wat je wil, maar je kan ooks ons helpen!";
            }
            StartCoroutine(QuestChanger());
        }
        else if (buildingCount >= 5)
        {
            BuildingSystem.onBuildingPlaced -= OnBuildingPlaced;
        }
    }
    //on slider change, this gets called, but since it only needs to be called when the user touches the slider for the first time it also unsubs from the slider
    public void SliderTutorialChange(float value)
    {
        slider.onValueChanged.RemoveListener(SliderTutorialChange);
        tutorialZoomStep = true;
        GameManager.paused = false;
        tutorialBuildStep = true;
        StartCoroutine(TimerAnimationStart());
        StartCoroutine(ResetAnimationStart());
    }
    //gets called when you press a button, keeping it here for now if i need that again so i dont need to go through the hassle of adding all the buttons again
    public void BuildingTutorialButton()
    {
        /*if (!tutorialBuildStep)
        {
            GameManager.paused = false;
            tutorialBuildStep = true;
            StartCoroutine(TimerAnimationStart());
            StartCoroutine(ResetAnimationStart());
        }*/
    }
    //does what it says on the tin, called when you finish the tutorial to change the tutorial box to the quest system
    IEnumerator QuestChanger()
    {
        yield return new WaitForSeconds(6);
        questBoxTextTMP.gameObject.SetActive(false);
        questSystem.SetActive(true);
    }
    //waits one second before deactivating everything but the nature reserve, so it doesnt happen on screen and ppl dont notice
    IEnumerator BuildingNatureReserveWaiter(string name)
    {
        yield return new WaitForSeconds(1);
        foreach (Transform child in ruralBuildings.transform)
        {
            if (child.gameObject.name != name)
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                child.gameObject.SetActive(true);
            }
        }
    }
    //same as above, just for activating everything
    IEnumerator BuildingActivationWaiter()
    {
        yield return new WaitForSeconds(1);
        foreach (Transform child in ruralBuildings.transform)
        {
            child.gameObject.SetActive(true);
        }
    }
    //making it glow at the start - doesnt work rn? just does it instantly
    /*IEnumerator QuestBoxFlash()
    {
        while (timer < 2)
        {
            Color color = questBoxImage.color;
            color.b -= 510 * Time.deltaTime;
            questBoxImage.color = color;
            timer += Time.deltaTime;
            yield return null;
        }

        timer = 0;
    }*/
    //everything below here is things moving
    IEnumerator SliderAnimationStart()
    {
        while (zoomSlider.transform.localPosition.x > 810)
        {
            Vector3 sliderAnimationPosition = zoomSlider.transform.localPosition;
            animationSpeed += 0.1f;
            sliderAnimationPosition.x -= 360f * animationSpeed * Time.deltaTime;
            if (sliderAnimationPosition.x <= 810)
            {
                animationSpeed = 1;
                tutorialRotationStep = true;
                sliderAnimationPosition.x = 810;
                zoomSlider.transform.localPosition = sliderAnimationPosition;
            }
            else
            {
                zoomSlider.transform.localPosition = sliderAnimationPosition;   
            }
            yield return null;
        }
    }
    IEnumerator TimerAnimationStart()
    {
        yield return new WaitForSeconds(tutorialDelaySeconds);
        while (timeImages.transform.localPosition.y > 470)
        {
            Vector3 timeAnimationPosition;
            timeAnimationPosition = timeImages.transform.localPosition;
            animationSpeed += 0.1f;
            timeAnimationPosition.y -= 360f * animationSpeed * Time.deltaTime;
            if (timeAnimationPosition.y <= 470)
            {
                animationSpeed = 1;
                timeAnimationPosition.y = 470;
                timeImages.transform.localPosition = timeAnimationPosition;
            }
            else
            {
                timeImages.transform.localPosition = timeAnimationPosition;   
            }
            yield return null;
        }
    }
    IEnumerator ResetAnimationStart()
    {
        yield return new WaitForSeconds(tutorialDelaySeconds);
        while (reset.transform.localPosition.x < -810)
        {
            Vector3 destroyAnimationPosition;
            destroyAnimationPosition = reset.transform.localPosition;
            animationSpeed += 0.1f;
            destroyAnimationPosition.x += 360f * animationSpeed * Time.deltaTime;
            if (destroyAnimationPosition.x >= -810)
            {
                animationSpeed = 1;
                destroyAnimationPosition.x = -810;
                reset.transform.localPosition = destroyAnimationPosition;
            }
            else
            {
                reset.transform.localPosition = destroyAnimationPosition;   
            }
            yield return null;
        }
    }
    IEnumerator NewsCasterAnimationStart()
    {
        while(announcerBox.transform.localPosition.x < -810)
        {
            Vector3 newsCasterAnimationPosition;
            newsCasterAnimationPosition = announcerBox.transform.localPosition;
            animationSpeed += 0.1f;
            newsCasterAnimationPosition.x += 360f * animationSpeed * Time.deltaTime;
            if (newsCasterAnimationPosition.x >= -810)
            {
                animationSpeed = 1;
                newsCasterAnimationPosition.x = -810;
                announcerBox.transform.localPosition = newsCasterAnimationPosition; 
            }
            else
            {
                announcerBox.transform.localPosition = newsCasterAnimationPosition;   
            }
            yield return null;
        }
    }
    //animation of the hand popping in, if youve completed the first tutorial step it doesnt activate at all potentially doesnt do anything anymore with the new tutorial
    IEnumerator HandAnimation()
    {
        yield return new WaitForSeconds(10);
        if(!tutorialRotationStep){
            tutorialArrow.SetActive(true);
            tutorialHand.SetActive(true);
            while (tutorialHand.transform.rotation.z > -0.6f)
            {
                tutorialHand.transform.Rotate(0, 0, -60*Time.deltaTime);
                yield return null;
                if (tutorialHand.transform.rotation.z <= -0.6f)
                {
                    tutorialHand.transform.rotation = Quaternion.Euler(0, 0, 15);
                }
            }
        }
    }
}