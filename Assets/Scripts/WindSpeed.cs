using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using System;

public class WindSpeed : MonoBehaviour
{
    public AudioMixer masterMixer;

    // High Frequency Wind Component
    public float hfwsMeanAmplitude;
    public float hfwsSDAmplitude;
    public float hfwsMeanPeriod;
    public float hfwsSDPeriod;
    private float hfwsCurrentSpeed;
    private float hfwsCurrentPeriod;
    private float hfwsDeltaSpeed;

    // Low Frequency Wind Component
    public float lfwsMeanAmplitude;
    public float lfwsSDAmplitude;
    public float lfwsMeanPeriod;
    public float lfwsSDPeriod;
    private float lfwsCurrentSpeed;
    private float lfwsCurrentPeriod;
    private float lfwsDeltaSpeed;

    // Gust Component

    public float gustMeanAmplitude;
    public float gustSDAmplitude;
    private float gustMeanPeriod;
    private float gustSDPeriod;
    private float gustCurrentSpeed = 0;
    private float gustCurrentPeriod;
    private float gustDeltaSpeed;
    public float gustAttackTime = 1;
    public float gustDecayTime = 2;

    // Gust switches
    private bool onGust = false;
    private bool onSquall = false;

    // Beaufort scale data
    private float[] WindForceScale;
    public int currentWindForce;

    private enum GustStatus
    {
        gustOff,
        gustWaiting,
        gustStarting,
        gustOn,
        gustClosing
    };

    private GustStatus gustStatus = GustStatus.gustOff; 


    // Start is called before the first frame update
    void Start()
    {
        WindForceScale = new float[] { 0.0f, 1.0f, 2.0f, 4.0f, 9.0f, 12.0f, 15.0f, 18.0f, 22.0f, 26.0f, 30.0f, 34.0f };
        SetLFWSpeed();
        SetHFWSpeed();
    }

    // Update is called once per frame
    void Update()
    {
        float frameTime = Time.deltaTime;

        // Check windspeed component periods finished

        lfwsCurrentPeriod -= frameTime;
        if(lfwsCurrentPeriod < 0.0f)
        {
            SetLFWSpeed();
        }
        hfwsCurrentPeriod -= frameTime;
        if (hfwsCurrentPeriod < 0.0f)
        {
            SetHFWSpeed();
        }

        // Set New Windspeed

        lfwsCurrentSpeed += lfwsDeltaSpeed * frameTime;
        hfwsCurrentSpeed += hfwsDeltaSpeed * frameTime;

        // Check Gust Status

        switch (gustStatus)
        {
            case GustStatus.gustOff:
                if (onGust)
                {
                    gustStatus = GustStatus.gustWaiting;
                    SetGustWaiting();
                }

                break;

            case GustStatus.gustWaiting:
                gustCurrentPeriod -= frameTime;
                if (gustCurrentPeriod < 0)
                {
                    SetGustStarting();
                    gustStatus = GustStatus.gustStarting;
                    
                }

                break;

            case GustStatus.gustStarting:
                gustCurrentPeriod -= frameTime;
                gustCurrentSpeed += gustDeltaSpeed * frameTime;
                if (gustCurrentPeriod < 0)
                {
                    SetGustOn();
                    gustStatus = GustStatus.gustOn;
                }
                

                break;

            case GustStatus.gustOn:
                gustCurrentPeriod -= frameTime;
                if (gustCurrentPeriod < 0)
                {
                    SetGustClosing();
                    gustStatus = GustStatus.gustClosing;
                }

                break;

            case GustStatus.gustClosing:
                gustCurrentPeriod -= frameTime;
                gustCurrentSpeed -= gustDeltaSpeed * frameTime;
                if (gustCurrentPeriod < 0)
                {
                    if (onGust)
                    {
                        gustStatus = GustStatus.gustWaiting;
                        gustCurrentSpeed = 0.0f;
                        SetGustWaiting();
                    }
                    else
                    {
                        gustStatus = GustStatus.gustOff;
                    }
                }
                


                break;

            default:
                break;
        }
        string text = "Gust Status " + gustStatus + " GustSpeed " + gustCurrentSpeed;
        Debug.Log(text);


        // Send to Plugin

        float totalWS = hfwsCurrentSpeed + lfwsCurrentSpeed + gustCurrentSpeed;
        if (totalWS > 30.0f)
        {
            totalWS = 30.0f;
        }

        if (totalWS < 0.1f)
        {
            totalWS = 0.1f;
        }
        masterMixer.SetFloat("WindSpeed", totalWS / 30.0f);
        masterMixer.SetFloat("DistantResonance", totalWS * 0.02f);
//        String text = "Wind Speed " + totalWS;
//        Debug.Log(text);

    }

    void SetLFWSpeed()
    {
        Tuple<float, float> randNos = BoxMuller();
        float newWSPeriod = lfwsMeanPeriod + lfwsSDPeriod * randNos.Item1;
        if (newWSPeriod < 0.1f)
        {
            newWSPeriod = 0.1f;
        }
        lfwsCurrentPeriod = newWSPeriod;
        float targetWS = lfwsMeanAmplitude + lfwsSDAmplitude * randNos.Item2;
        lfwsDeltaSpeed = (targetWS - lfwsCurrentSpeed) / newWSPeriod;
    }

    void SetHFWSpeed()
    {
        Tuple<float, float> randNos = BoxMuller();
        float newWSPeriod = hfwsMeanPeriod + hfwsSDPeriod * randNos.Item1;
        if (newWSPeriod < 0.1f)
        {
            newWSPeriod = 0.1f;
        }
        hfwsCurrentPeriod = newWSPeriod;
        float targetWS = hfwsMeanAmplitude + hfwsSDAmplitude * randNos.Item2 * (1 + gustCurrentSpeed * 0.1f);
        hfwsDeltaSpeed = (targetWS - hfwsCurrentSpeed) / newWSPeriod;
    }

    void SetGustWaiting()
    {
        Tuple<float, float> randNos = BoxMuller();
        float newGustPeriod = gustMeanPeriod * 10.0f + gustSDPeriod * randNos.Item1;

        if (newGustPeriod < 2.0f)
        {
            newGustPeriod = 2.0f;
        }
        gustCurrentPeriod = newGustPeriod;
    }

    void SetGustStarting()
    {
        Tuple<float, float> randNos = BoxMuller();
        float newGustPeriod = gustAttackTime + gustSDPeriod * randNos.Item1;
        
        if (newGustPeriod < 0.1f)
        {
            newGustPeriod = 0.1f;
        }
        Debug.Log(newGustPeriod);
        gustCurrentPeriod = newGustPeriod;
        float targetGustWS = gustMeanAmplitude + gustSDAmplitude * randNos.Item2;
        gustDeltaSpeed = (targetGustWS - gustCurrentSpeed) / newGustPeriod;
    }

    void SetGustOn()
    {
        Tuple<float, float> randNos = BoxMuller();
        float newGustPeriod = gustMeanPeriod + gustSDPeriod * randNos.Item1 * gustMeanPeriod;
        string text = "Mean " + gustMeanPeriod + " SD " + gustSDPeriod + " , " + randNos.Item1;
        Debug.Log(text);

        if (onSquall)
        {
            newGustPeriod *= 5.0f;
        }
        if (newGustPeriod < 1.0f)
        {
            newGustPeriod = 1.0f;
        }
        gustCurrentPeriod = newGustPeriod;
        Debug.Log(gustCurrentPeriod);

    }

    void SetGustClosing()
    {
        gustCurrentPeriod = gustDecayTime;
        gustDeltaSpeed = gustCurrentSpeed / gustCurrentPeriod;
    }

    Tuple<float, float> BoxMuller()
    {
        // Returns 2 float as a normal distribution
        // 
        float u1 = 1.0f - UnityEngine.Random.value; 
        float u2 = 1.0f - UnityEngine.Random.value;
        return new Tuple<float, float>(
            Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2)
            , Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2)); 
        
    }

    // Setters For Sliders

    public void SetMeanWSPeriod(float Value)
    {
        lfwsMeanPeriod = Value;
    }

    public void SetSDWSPeriod(float Value)
    {
        lfwsSDPeriod = Value;
    }

    public void SetWindForce(float Value)
    {
        lfwsMeanAmplitude = WindForceScale[(int)Value];
        lfwsSDAmplitude = lfwsMeanAmplitude / 8.0f;
    }

    public void SetGustMeanPeriod(float value)
    {
        gustMeanPeriod = value;
    }

    public void SetGustSDPeriod(float value)
    {
        gustSDPeriod = value;
    }

    public void SetOnGust(bool Value)
    {
        onGust = Value;
    }

    public void SetOnSquall(bool Value)
    {
        onSquall = Value;
    }
}
