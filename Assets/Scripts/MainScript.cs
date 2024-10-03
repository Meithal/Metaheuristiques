using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class MainScript : MonoBehaviour
{
    public Transform[] cubes;

    // Start is called before the first frame update
    private IEnumerator Start()
    {
        Debug.Log($"Il y a {cubes.Length} cubes dans cubes !");
        Debug.Log($"Erreur avant mélange : {GetError()}");
        //yield return StartCoroutine(ShuffleCubes());
        Debug.Log($"Erreur après mélange : {GetError()}");

        // yield return new WaitForSeconds(2f);

        // yield return StartCoroutine(NaiveLocalSearch());
        // yield return StartCoroutine(SimulatedAnnealing());
        yield return StartCoroutine(GeneticAlgorithm());
    }

    private IEnumerator ShuffleCubes()
    {
        for (var i = 0; i < cubes.Length; i++)
        {
            var rdmIndex = Random.Range(i, cubes.Length);
            SwapCubePositions(cubes[i], cubes[rdmIndex]);
            yield return null;
        }
    }
    
    private void InstantShuffleCubes()
    {
        for (var i = 0; i < cubes.Length; i++)
        {
            var rdmIndex = Random.Range(i, cubes.Length);
            SwapCubePositions(cubes[i], cubes[rdmIndex]);
        }
    }

    private int GetError()
    {
        var error = 0;

        foreach (var cube1 in cubes)
        {
            foreach (var cube2 in cubes)
            {
                if (cube1.CompareTag(cube2.tag) &&
                    Math.Abs(cube1.position.y - cube2.position.y) > 0.000001f)
                {
                    error += 1;
                }
            }
        }

        return error;
    }

    private static void SwapCubePositions(Transform cube1, Transform cube2)
    {
        (cube1.position, cube2.position) = (cube2.position, cube1.position);
    }

    private IEnumerator NaiveLocalSearch()
    {
        var currentError = GetError();

        while (currentError != 0)
        {
            Transform cube1;
            Transform cube2;

            do
            {
                cube1 = cubes[Random.Range(0, cubes.Length)];
                cube2 = cubes[Random.Range(0, cubes.Length)];
            } while (!AreNeighbours(cube1, cube2));
            
            SwapCubePositions(cube1, cube2);

            var newError = GetError();

            if (currentError < newError)
            {
                SwapCubePositions(cube1, cube2);
            }
            else
            {
                currentError = newError;
            }

            yield return null;
        }
    }

    private static bool AreNeighbours(Transform cube1, Transform cube2)
    {
        // Côte à côte
        if (Math.Abs(cube1.position.y - cube2.position.y) < 0.0001 &&
            Math.Abs(Mathf.Abs(cube1.position.x - cube2.position.x) - 2f) < 0.0001f)
        {
            return true;
        }
        
        // L'un au dessus de l'autre
        if (Math.Abs(cube1.position.x - cube2.position.x) < 0.0001 &&
            Math.Abs(Mathf.Abs(cube1.position.y - cube2.position.y) - 2f) < 0.0001f)
        {
            return true;
        }

        return false;
    }

    private IEnumerator SimulatedAnnealing()
    {
        var initialTemperature = 7f;
        var temperature = initialTemperature;
        var stagnation = 0;
        var stagnationLimit = 2000;
        
        var currentError = GetError();
        var testCount = 1;

        while (currentError != 0)
        {
            var previousError = currentError;
            Transform cube1;
            Transform cube2;

            do
            {
                cube1 = cubes[Random.Range(0, cubes.Length)];
                cube2 = cubes[Random.Range(0, cubes.Length)];
            } while (!AreNeighbours(cube1, cube2));
            
            SwapCubePositions(cube1, cube2);

            var newError = GetError();
            testCount += 1;

            var rdm = Random.Range(0f, 1f);
            if (rdm <= MetropolisCriterium(temperature, currentError, newError))
            {
                currentError = newError;
            }
            else
            {
                SwapCubePositions(cube1, cube2);
            }

            if (previousError == currentError)
            {
                stagnation += 1;
            }
            else
            {
                stagnation = 0;
            }

            if (stagnation > stagnationLimit)
            {
                temperature = initialTemperature;
            }

            // temperature -= 0.001f; // décroissance linéaire
            temperature *= 0.999f; // décroissance géométrique
            Debug.Log($"temperature : {temperature}, stagnation : {stagnation}");
            yield return null;
        }

        yield return null;
        
        Debug.Log($"Optimal solution found in {testCount} tests");
    }

    private float MetropolisCriterium(float temperature, 
        int currentError,
        int newError)
    {
        if (temperature <= 0)
        {
            return currentError >= newError ? 1f : 0f;
        }
        
        return Mathf.Exp((currentError - newError) / temperature);
    }

    private IEnumerator GeneticAlgorithm()
    {
        var popSize = 200;
        var breedersPercentage = 0.2f;
        var breedersCount = Mathf.FloorToInt(breedersPercentage * popSize);
        var mutationRate = 0.5f;
        var generationId = 1;
        
        // INITIALISATION DE LA POPULATION
        var population = new Vector3[popSize][];

        for (var i = 0; i < popSize; i++)
        {
            InstantShuffleCubes();
            
            var solution = new Vector3[cubes.Length];
            for (var j = 0; j < cubes.Length; j++)
            {
                solution[j] = cubes[j].position;
            }

            population[i] = solution;
        }

        while (true)
        {
            // EVALUATION DE LA POPULATION
            var scoredPopulation = new Dictionary<Vector3[], int>(popSize);
            for (int i = 0; i < popSize; i++)
            {
                for (int j = 0; j < cubes.Length; j++)
                {
                    cubes[j].position = population[i][j];
                }

                var score = GetError();
                scoredPopulation.Add(population[i], score);
            }

            // SELECTION DES REPRODUCTEURS
            var breeders = scoredPopulation
                .OrderBy(kv => kv.Value)
                .Select(kv => kv.Key)
                .Take(breedersCount)
                .ToArray();

            // AFFICHAGE DU MEILLEUR INDIVIDU
            for (int j = 0; j < cubes.Length; j++)
            {
                cubes[j].position = breeders[0][j];
            }

            if (GetError() == 0)
            {
                break;
            }

            // CROISEMENT
            var newPopulation = new Vector3[popSize][];
            for (int i = 0; i < popSize; i++)
            {
                var p1 = breeders[Random.Range(0, breedersCount)];
                var p2 = breeders[Random.Range(0, breedersCount)];

                newPopulation[i] = CrossOverAlternate(p1, p2);
            }


            // MUTATION
            for (int i = 0; i < popSize; i++)
            {
                var rnd = Random.Range(0f, 1f);
                if (rnd <= mutationRate)
                {
                    var pos1 = Random.Range(0, cubes.Length);
                    var pos2 = Random.Range(0, cubes.Length);

                    (newPopulation[i][pos1], newPopulation[i][pos2]) = (newPopulation[i][pos2], newPopulation[i][pos1]);
                }
            }

            population = newPopulation;
            generationId++;
            Debug.Log(generationId);

            yield return null;
        }
        
        Debug.Log($"Nombre de tests faits pour trouver la pop optimale : {popSize * generationId}");
    }


    private Vector3[] CrossOverAlternate(Vector3[] p1, Vector3[] p2)
    {
        var child = new Vector3[cubes.Length];

        for (int j = 0; j < cubes.Length; j++)
        {
            child[j] = new Vector3(358f, 832f, 42.42f);
        }
        
        bool lookingAtP1 = true;
        var parentCounter = 0;

        for (int j = 0; j < cubes.Length; j++)
        {
            if (lookingAtP1)
            {
                if (!child.Contains(p1[parentCounter]))
                {
                    child[j] = p1[parentCounter];
                    lookingAtP1 = !lookingAtP1;
                }
                else if(!child.Contains((p2[parentCounter])))
                {
                    child[j] = p2[parentCounter];
                }
                else
                {
                    j--;
                }
            }
            else
            {
                if (!child.Contains(p2[parentCounter]))
                {
                    child[j] = p2[parentCounter];
                    lookingAtP1 = !lookingAtP1;
                }
                else if(!child.Contains((p1[parentCounter])))
                {
                    child[j] = p1[parentCounter];
                }
                else
                {
                    j--;
                }

            }
            
            
        }
         
        
        return child;
    }
}