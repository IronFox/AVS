using Nautilus.Crafting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AVS.Log;

namespace AVS.Configuration;

/// <summary>
/// Ingredient of a recipe, including its type and quantity.
/// </summary>
public readonly struct RecipeIngredient
{
    /// <summary>
    /// Type of the ingredient.
    /// </summary>
    public TechType Type { get; }

    /// <summary>
    /// Amount of the ingredient required for the recipe.
    /// </summary>
    public int Amount { get; }

    /// <summary>
    /// Constructs an ingredient
    /// </summary>
    /// <param name="type">The type of the ingredient, represented as a <see cref="TechType"/>.</param>
    /// <param name="amount">The quantity of the ingredient required. Must be a positive, non-zero integer.</param>
    public RecipeIngredient(TechType type, int amount)
    {
        Type = type;
        Amount = amount;
        if (Amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be a positive integer.");
    }
}

/// <summary>
/// Recipe builder for creating complex recipes with multiple ingredients.
/// </summary>
public sealed class RecipeBuilder
{
    private Dictionary<TechType, int> Ingredients { get; } = new();

    internal RecipeBuilder()
    {
    }

    private void AddOne(RecipeIngredient ingredient)
    {
        if (ingredient.Type == TechType.None || ingredient.Amount <= 0)
            return;
        if (Ingredients.TryGetValue(ingredient.Type, out var existingAmount))
            Ingredients[ingredient.Type] = existingAmount + ingredient.Amount;
        else
            Ingredients[ingredient.Type] = ingredient.Amount;
    }

    internal RecipeBuilder AddRange(IEnumerable<RecipeIngredient> ingredients)
    {
        foreach (var ingredient in ingredients) AddOne(ingredient);
        return this;
    }

    /// <summary>
    /// Adds the ingredients from the specified <see cref="Recipe"/> to the current recipe.
    /// </summary>
    /// <param name="recipe">Recipe to add ingredients of</param>
    /// <returns>this</returns>
    public RecipeBuilder Add(IEnumerable<RecipeIngredient> recipe) => AddRange(recipe);

    /// <summary>
    /// Combines the current recipe with another recipe by merging their ingredients.
    /// </summary>
    /// <param name="other">The recipe to combine with the current recipe. Must not be <see langword="null"/>.</param>
    /// <returns>A new <see cref="RecipeBuilder"/> instance containing the combined ingredients of both recipes.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is <see langword="null"/>.</exception>
    public RecipeBuilder Include(RecipeBuilder other)
    {
        if (other is null)
            throw new ArgumentNullException(nameof(other), "Other recipe must not be null");
        return AddRange(other.Ingredients.Select(i => new RecipeIngredient(i.Key, i.Value)));
    }


    /// <summary>
    /// Adds one unit of each specified <see cref="TechType"/> to the recipe.
    /// </summary>
    /// <param name="types">A collection of <see cref="TechType"/> values to add to the recipe. Any <see cref="TechType.None"/> values
    /// in the collection are ignored.</param>
    /// <returns>The updated <see cref="RecipeBuilder"/> instance, allowing for method chaining.</returns>
    public RecipeBuilder AddOneOfEach(IEnumerable<TechType> types)
    {
        foreach (var type in types)
            if (type != TechType.None)
                AddOne(new RecipeIngredient(type, 1));

        return this;
    }


    /// <summary>
    /// Adds a specified ingredient and its quantity to the recipe.
    /// </summary>
    /// <remarks>If <paramref name="type"/> is <see cref="TechType.None"/> or <paramref
    /// name="amount"/> is less than or equal to zero,  the method does nothing and returns the current
    /// instance.</remarks>
    /// <param name="type">The type of ingredient to add. Must not be <see cref="TechType.None"/>.</param>
    /// <param name="amount">The quantity of the ingredient to add. Must be greater than zero.</param>
    /// <returns>The current <see cref="RecipeBuilder"/> instance, allowing for method chaining.</returns>
    public RecipeBuilder Add(TechType type, int amount)
    {
        if (type == TechType.None || amount <= 0)
            return this;
        AddOne(new RecipeIngredient(type, amount));
        return this;
    }

    /// <summary>
    /// Adds an ingredient to the recipe with the specified type and amount.
    /// </summary>
    /// <param name="ingredient">A tuple containing the type of the ingredient and the amount to add.  <paramref name="ingredient.Type"/>
    /// specifies the ingredient type, and  <paramref name="ingredient.Amount"/> specifies the quantity to add.</param>
    /// <returns>The current <see cref="RecipeBuilder"/> instance, allowing for method chaining.</returns>
    public RecipeBuilder Add((TechType Type, int Amount) ingredient) => Add(ingredient.Type, ingredient.Amount);


    /// <summary>
    /// Adds the specified ingredient to the recipe.
    /// </summary>
    /// <param name="ingredient">The ingredient to add, including its type and amount.</param>
    /// <returns>A <see cref="RecipeBuilder"/> instance with the ingredient added, allowing for method chaining.</returns>
    public RecipeBuilder Add(RecipeIngredient ingredient) => Add(ingredient.Type, ingredient.Amount);


    /// <summary>
    /// Adds a <see cref="RecipeIngredient"/> to the <see cref="RecipeBuilder"/> and returns the updated builder.
    /// </summary>
    /// <param name="builder">The <see cref="RecipeBuilder"/> to which the ingredient will be added.</param>
    /// <param name="ingredient">The <see cref="RecipeIngredient"/> to add to the builder.</param>
    /// <returns>The updated <see cref="RecipeBuilder"/> instance with the added ingredient.</returns>
    public static RecipeBuilder operator +(RecipeBuilder builder, RecipeIngredient ingredient)
    {
        builder.AddOne(ingredient);
        return builder;
    }

    /// <summary>
    /// Adds an ingredient to the recipe using the specified <see cref="TechType"/> and amount.
    /// </summary>
    /// <remarks>This operator provides a convenient way to add ingredients to a recipe by using the
    /// <c>+</c> operator.</remarks>
    /// <param name="builder">The <see cref="RecipeBuilder"/> instance to which the ingredient will be added.</param>
    /// <param name="ingredient">A tuple containing the <see cref="TechType"/> of the ingredient and the amount to add. The first item
    /// represents the type of the ingredient, and the second item represents the quantity.</param>
    /// <returns>A new <see cref="RecipeBuilder"/> instance with the specified ingredient added.</returns>
    public static RecipeBuilder operator +(RecipeBuilder builder, (TechType Type, int Amount) ingredient) =>
        builder.Add(ingredient.Type, ingredient.Amount);

    /// <summary>
    /// Constructs a new <see cref="Recipe"/> instance using the specified ingredients.
    /// </summary>
    /// <returns>A <see cref="Recipe"/> object containing the provided ingredients.</returns>
    public Recipe Done() => new(Ingredients);
}

/// <summary>
/// Sequential builder for a <see cref="Recipe"/>.
/// </summary>
public static class NewRecipe
{
    /// <summary>
    /// Creates a new instance of the <see cref="NewRecipe"/> class with no ingredients.
    /// </summary>
    /// <returns>A new <see cref="NewRecipe"/> instance.</returns>
    public static RecipeBuilder FromNothing() => new();

    /// <summary>
    /// Creates a new <see cref="NewRecipe"/> instance from the specified <see cref="Recipe"/>.
    /// </summary>
    /// <param name="recipe">The source <see cref="Recipe"/> to convert. Must not be <see langword="null"/>.</param>
    /// <returns>A new <see cref="NewRecipe"/> instance containing the ingredients from the specified <see cref="Recipe"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="recipe"/> is <see langword="null"/>.</exception>
    public static RecipeBuilder From(Recipe recipe)
    {
        if (recipe is null)
            throw new ArgumentNullException(nameof(recipe), "Recipe must not be null");
        var n = new RecipeBuilder();
        return n.AddRange(recipe);
    }


    /// <summary>
    /// Creates a new instance of the <see cref="NewRecipe"/> class with an initial ingredient.
    /// </summary>
    /// <param name="type">The type of the ingredient to add to the recipe.</param>
    /// <param name="amount">The quantity of the ingredient to add. Must be a positive integer.</param>
    /// <returns>A new <see cref="NewRecipe"/> instance containing the specified ingredient.</returns>
    public static RecipeBuilder Add(TechType type, int amount) =>
        new RecipeBuilder().Add(new RecipeIngredient(type, amount));

    /// <summary>
    /// Creates a new instance of <see cref="NewRecipe"/> with the specified ingredient.
    /// </summary>
    /// <param name="ingredient">A tuple containing the ingredient's <see cref="TechType"/> and the amount to be added. The <see
    /// cref="TechType"/> specifies the type of the ingredient, and the amount must be a positive integer.</param>
    /// <returns>A new <see cref="NewRecipe"/> instance with the specified ingredient added.</returns>
    public static RecipeBuilder Add((TechType Type, int Amount) ingredient) =>
        new RecipeBuilder().Add(ingredient.Type, ingredient.Amount);
}

/// <summary>
/// Readonly vehicle construction recipe.
/// </summary>
public class Recipe : IEnumerable<RecipeIngredient>, IEquatable<Recipe>
{
    /// <summary>
    /// Gets an example recipe that demonstrates the required ingredients for crafting.
    /// </summary>
    public static Recipe Example { get; } =
        NewRecipe
            .Add(TechType.Titanium, 2)
            .Add(TechType.Quartz, 1)
            .Add(TechType.PowerCell, 1)
            .Done();

    /// <summary>
    /// Gets an empty recipe with no ingredients.
    /// </summary>
    // ReSharper disable once UseCollectionExpression
    public static Recipe Empty { get; } = new(Array.Empty<RecipeIngredient>());

    /// <summary>
    /// Converts a <see cref="RecipeData"/> to a <see cref="Recipe"/>.
    /// </summary>
    /// <param name="recipeData">Data to import</param>
    /// <param name="fallback">Fallback recipe to return in case of invalid data</param>
    /// <returns>Imported data</returns>
    public static Recipe Import(RecipeData recipeData, Recipe fallback)
    {
        if (recipeData.Ingredients is null
            || recipeData.Ingredients.Count == 0
           )
        {
            LogWriter.Default.Error("RecipeData is null or has no ingredients. Returning fallback Recipe.");
            return fallback ?? throw new ArgumentNullException(nameof(fallback), "fallback must not be null");
        }

        if (recipeData.craftAmount != 1)
        {
            LogWriter.Default.Error("RecipeData produces amounts other than 1. Returning fallback Recipe.");
            return fallback ?? throw new ArgumentNullException(nameof(fallback), "fallback must not be null");
        }

        if (recipeData.linkedItemCount != 1)
        {
            LogWriter.Default.Error("RecipeData has non-empty linked item count. Returning fallback Recipe.");
            return fallback ?? throw new ArgumentNullException(nameof(fallback), "fallback must not be null");
        }

        return new Recipe(
            recipeData.Ingredients.Select(ingredient => new RecipeIngredient(ingredient.techType, ingredient.amount)
            ));
    }


    private Dictionary<TechType, int> IngredientsDictionary { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="Recipe"/> class with the specified ingredients.
    /// </summary>
    /// <remarks>If multiple ingredients of the same type are provided, the last one in the collection
    /// will overwrite the previous entries in the internal dictionary.</remarks>
    /// <param name="ingredients">A collection of <see cref="RecipeIngredient"/> objects representing the ingredients and their amounts. Each
    /// ingredient's type will be used as a key in the internal dictionary.</param>
    public Recipe(IEnumerable<RecipeIngredient> ingredients)
    {
        foreach (var ingredient in ingredients) IngredientsDictionary[ingredient.Type] = ingredient.Amount;
    }

    internal Recipe(Dictionary<TechType, int> ingredients)
    {
        IngredientsDictionary = ingredients;
    }

    /// <summary>
    /// Gets a value indicating whether the collection of ingredients is empty.
    /// </summary>
    public bool IsEmpty => IngredientsDictionary.Count == 0;

    /// <summary>
    /// Converts the current object to a <see cref="RecipeData"/> instance.
    /// </summary>
    /// <remarks>The method creates a new <see cref="RecipeData"/> object and populates its
    /// ingredients list based on the current object's <c>IngredientsDictionary</c>. Each entry in the dictionary is
    /// transformed into a <see cref="CraftData.Ingredient"/> and added to the resulting recipe.</remarks>
    /// <returns>A <see cref="RecipeData"/> instance containing the ingredients from the current object's
    /// <c>IngredientsDictionary</c>.</returns>
    public RecipeData ToRecipeData()
    {
        var recipeData = new RecipeData();
        foreach (var ingredient in IngredientsDictionary)
            // ReSharper disable once ArrangeObjectCreationWhenTypeNotEvident
            recipeData.Ingredients.Add(new(ingredient.Key, ingredient.Value));
        return recipeData;
    }

    /// <inheritdoc />
    public IEnumerator<RecipeIngredient> GetEnumerator()
    {
        foreach (var kvp in IngredientsDictionary) yield return new RecipeIngredient(kvp.Key, kvp.Value);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Checks if the recipe is valid for vehicle registration.
    /// </summary>
    /// <param name="vehicleName"></param>
    /// <returns></returns>
    public bool CheckValidity(string vehicleName)
    {
        var badRecipeFlag = false;
        foreach (var ingredient in this)
            try
            {
                if (ingredient.Type.EncodeKey() is null)
                    throw new KeyNotFoundException($"TechType {ingredient.Type} is not registered or does not exist.");
            }
            catch (Exception e)
            {
                Logger.LogException(
                    $"Vehicle Recipe Error: {vehicleName}'s recipe had an invalid tech type: {ingredient.Type}. Probably you are referencing an unregistered/non-existent techtype",
                    e);
                badRecipeFlag = true;
            }

        return !badRecipeFlag;
    }

    /// <inheritdoc />
    public bool Equals(Recipe other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (IngredientsDictionary.Count != other.IngredientsDictionary.Count)
            return false;

        foreach (var kvp in IngredientsDictionary)
        {
            if (!other.IngredientsDictionary.TryGetValue(kvp.Key, out var otherAmount))
                return false;
            if (kvp.Value != otherAmount)
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is Recipe otherRecipe)
            return Equals(otherRecipe);
        return false;
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = 17;
        foreach (var kvp in IngredientsDictionary.OrderBy(k => k.Key))
        {
            hash = hash * 31 + kvp.Key.GetHashCode();
            hash = hash * 31 + kvp.Value.GetHashCode();
        }

        return hash;
    }

    /// <summary>
    /// Determines whether two <see cref="Recipe"/> instances are equal.
    /// </summary>
    /// <param name="left">The first <see cref="Recipe"/> instance to compare.</param>
    /// <param name="right">The second <see cref="Recipe"/> instance to compare.</param>
    /// <returns><c>true</c> if the specified <see cref="Recipe"/> instances are equal; otherwise, <c>false</c>.</returns>
    public static bool operator ==(Recipe left, Recipe right) => Equals(left, right);

    /// <summary>
    /// Determines whether two <see cref="Recipe"/> instances are not equal.
    /// </summary>
    /// <param name="left">The first <see cref="Recipe"/> instance to compare.</param>
    /// <param name="right">The second <see cref="Recipe"/> instance to compare.</param>
    /// <returns>
    /// <c>true</c> if the two <see cref="Recipe"/> instances are not equal; otherwise, <c>false</c>.
    /// </returns>
    public static bool operator !=(Recipe left, Recipe right) => !Equals(left, right);

    /// <inheritdoc />
    public override string ToString()
    {
        return string.Join(", ", IngredientsDictionary.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
    }
}