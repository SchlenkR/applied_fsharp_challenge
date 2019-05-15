
# Digital Signal Processing with F# (in the Domain of Audio and Music)

## Einleitung und Motivation

... 
Key point: Eigentlich denkt man: OO,  ...this leads to - well, it lead me to pure functions!
"Well, it lead me to pure functional programming, and we will see why it is superior to an object-oriented approach when we want to express stateful computations.

## Definition und Abgrenzung

* Audio signals (means: at the end, we are interested in a sequence of float values)

Was ist DSP

### Digital
Non-Continuous: What does it mean: Take an analog synthesizer. It creates and outputs a signal based on electric currency - which is continuous from a physical point of view. A computer cannot process values in a continuous way - it has to **quantize** two things:
    * Time: This is called sampling, and happens for an audio signal usually at multiples of 44100 times per second (44.1 kHz). Why 44100? Look at TODO: Sample Theorem.
    * Values: At each sample point, a value must be captured (analog to the electric currency). This happens usually in a number, represented by a 16, 32 or 64 bit value. In this article, we use a F# "float" value, which is a 64 bit floating point number.

### Signal
Having understood the definition from above, it's easy to define what a signal is: A signal is *a value that changes over time*. This means: you could take a sequence of time-related-values like this to represent a digital signal:

[ (0.0<s>, 0.0); (0.1<s>, 0.3); ... ]
(where the 1st tuple element is the time and the second is the value.)

Plotting this time-value sequence leads to this:
TODO: Plot

As we can see, we have captured a sine wave with the *amplitude* of 0.5 (see here TODO) and a frequence of 1Hz (1 complete cycle in 1s). The sample rate is 16Hz. Why? Because we have captured 16 samples in 1 second.

Given the information that we have a sample rate of 16Hz, we can simplify our time-value sequence to just a value sequence:
[ 0.0; 0.3; ...]

The point-in-time of the the n-th value in the sequence can easily be calculated when sample rate and starting time are given. This fact is fundamental, because at the end, we are just dealing with a sequence of values - which sounds easy and actually is!

### Processing

(**
According to the Cambridge Dictionary (https://dictionary.cambridge.org/dictionary/english/processing), processing is "the use of a computer to collect, store, organize, and use information".

In our case, information is represented by sampled values, organized in a sequence and used to produce effects such as visualization (plotting) or audio-output (speaker).

So, from where does the information we are talking about come from?
**)

* Creation of a new signal
* Alteration of an existing signal

From a practical point of view, distinguishing between creation and alteration is not relevant because creation is a special case of alteration, which we will see shortly.

But at first, let's look at *alteration*:

Given this input signal (a sine wave):
[TODO: Sinus]

Now we want to 'amplify' the sine wave in a way that the amplitude is not 0.5 anymore, but 1. This is easy since for each value in our input sequence, we can define a function that maps an input value to an output value by multiplication with 2.0:

```fsharp
// float -> float
let amplifyBy2 inputValue = inputValue * 2.0
```

Now we assume there exists a 'helper' function that can take the 'amplify'-function and a input-value-sequence to produce the output-value-sequence. This function is called 'map' and can be used in this manner:

```fsharp
let outputSequence = map amplifyBy2 [TODO: Input]
```

[ 1; 2; 3 ]   input
  |  |  |
  |  |  | --- amplifyBy2 transforms each value of input
  v  v  v
[ 2; 4; 6 ]   output


**Important note:** This function operates on a single value, which means no 'past state or past input' is needed to perform the calculation. So it 'knows' nothing about the 'outer world', which means it has only ...

Let's have a look at creating a sequence of values. As stated before, creation of a value-sequence is a special case of alteration, because of this:

Alteration can be defined as "A transformation of n float-input-values into n float-output-values by applying a mapping function to each input-value". 

Creation can be defined as "A transformation of n 'nothing'-input-values into n float-output-values by applying a mapping function to each input-value".

That sounds quite similar. How does this work, and: Can it work?

In F#, there is a type that represents nothing which is the 'unit'-type. The unit type has one instance, which is represented by '()'. To have a single character, we define an alias for '()' called 'o'.

[ o; o; o ]   input
  |  |  |
  |  |  | --- what is the transformation function?
  v  v  v
[ a; b; c ]   output

Now it comes to an interesting aspect of 'pure' functional programming: In a 'pure' functional program, there is no concept of variables (or mutating state). There is nothing like true randomness which means that a function evaluates to the exact same value as in previous evaluations when the input values of that function are the same. A function itself cannot preserve state that is spanned over evaluations, and it cannot 'break out' of it's scope to call some random-number-generator or something. From an inner perspective of a function, the whole 'universe' of that function is the values that are visible to it (by passing them or having values captured by closure). This function has no identity; it doesn't remember anything from one evaluation to the next. At first view, this seems to be very limited compared to an imperative / object oriented approach with all it's changing state. And it seems like this abstraction doesn't fit well with our physical world where the current state is a product of all it's past states. We will revisit this later and see that it is not true.

However, having this limitation in mind, it's easy to understand that a sequence of 'nothings' can never be transformed in a sequence of values that differ from each other. It could only be transformed in (for example) a sequence of '3's. Here is an example:

```fsharp
let transformNothingToSomething () -> 3
```

This results in:

[ o; o; o ]   input
  |  |  |
  |  |  | --- transformNothingToSomething
  v  v  v
[ 3; 3; 3 ]   output

So how can it be possible to generate something like a sine wave out of nothing? Answer: It is not possible ("and why there is something at all and not nothing... I don't know."). To create a sine wave, there are more 'outer' values necessary than just nothing, which is evident when you look at how the sine function is defined: it needs an angle. This angle is in our case proportional to a point-in-time. With this insight, we can redefine what *creation* of signals mean:

"A transformation of n 'time'-input-values into n float-output-values by applying a mapping function to each input-value". In addition to the current 'time' value, we could also pass in the sample rate 'sr'.

```fsharp
let sin (sr,time) -> Math.Sin time // TODO: Das hier korrekt machen
```

[ 1.0; 2.0; 3.0 ]   input
   |    |    | 
   |    |    |  --- sin
   v    v    v
[ 0.0; 0.8; 0.9 ]  output


## 


Another example: Delay: Seed value

// When we capture ("sample") the value with a fixed step size (sample rate)


"Warum ist nicht nichts"

Ok, that was easy. But remember what we have said before: State!
-> OOP vs FP


* Alteration: Note that input values doesn't have to be a scalar value - it can also be a tuple or whatever.

Pure: Random genreator - das sieht zuerst wie ein Hindernis aus. But: Restriction that helps us.

Time: Dynamic process.

Arten:
    * Erzeugung von Signalen
    * Änderung von Signalen

* In the time domain (in contrast to frequency domain)

---
Hier: Im Kontext von Audio-Signalen
    Was ist ein Audio-Signal?

(Anmerkung: We normalize all of our values to a range from -1.0 to +1.0.)

Abgrenzung: AudioRate / ControlRate
    Kann man prinzipiell beides machen. Hier geht es jedoch nicht um Control-Signale, sondern um Audio-Signale

Physik:
    Oszillatoren, Filter - diese sind ganz prinzipiell State-behaftet. In unserer realen Welt passieren Dinge nicht einfach so. Vorgänge basieren aufeinander - Aktio und Reaktio - und das passiert wiederum deterministisch oder statistisch. Aus einer globalen Sicht hängen alle "Dinge" (dazu zählen Entitäten, deren Zustand sowie Vorgänge) voneinander ab und eine Möglichkeit, dies zu modellieren, ist diese: Es existiert eine Reihe von Entitäten, die einen oder mehrere Zustände haben. Übergänge von einem "Moment" zum anderen werden beschrieben als Änderungen dieser Zustände durch Berechnungen. Der nächste Moment wiederum ist auf die gleiche Weise zu ermitteln; diesmal jedoch auf dem eben geänderten Zustand. Dies kann fortlaufend - also iterativ - durchgeführt werden - e voila: Wir haben eine einschrittige Simulation.

    Anschauliches Beispiel: Bild mit Auto: Wo ist es im nächsten Frame? Das kann man nur wissen, wenn man den Zustand des Autos kennt, genauer: Dessen Geschwindigkeit. 


Sinus
    Kann man als Funktion über die Zeit darstellen
    Problem: Interaktionen - Änderung der Frequenz (Modulation)
    Bild: "Sprünge, wenn sich die Frequenz ändert


* Es geht
* Kurze Erklärung an einem Beispiel. Dann: Das kann man toll mit OO machen, denkt man. Aber das Gegenteil ist der Fall!


OOP APproach: Instanciation and calling process() sucks. we wanna get rid of this.
look in our diagram: there is just a flow defined; instanciation is 'inlined' with the flow.

