
# Digital Signal Processing with F# (in the Domain of Audio and Music)

## Einleitung und Motivation

Making music with the help of computers is not a new approach. It started with music N (TODO: Nochmal ausbauen)...
In the analog world, physical quantities like the electric currency are used to represent a signal that is finally sent to a speaker. Thes quantities are created and altered by low-level components like condensors, resistors, magnetic coils, transistors, diods, etc. which are connected to each others in circuits. They are composed to larger components like operational amplifiers, that are again used to build modules like filters and oscillators which synthesizers and effect processors are made of. Digital audio signal processing (Audio-DSP) is about modeling these components on different levels of abstraction, resulting in the simulation of circuits similar to their analog counterparts (of course, there is no limit in creating completely new forms of synthesizers and effects).

If you are already familiar with languages like Java or C#, you might think that DSP fits perfectly in the world of object-oriented programming languages: With it's basic idea of encapsulating mutable state by behavior (methods) and giving it an addressable identity (instance pointer), it maps directly to the concept of having physical entities (like condensors or transistors) that "exist" and hold their's own state that changes over time. This article is about an alternative approach of audio-DSP which is solely based on the paradigm of pure functions using a language called F#. TODO: Benefits

(******
We will learn that object-oriented concept like instances (and therfore instanciation) as well as aspects of method evaluation can be left out completely, leading to a way of describing signal flows and circuits purely focussing on how values flow through the system.

Key point: Eigentlich denkt man: OO,  ...this leads to - well, it lead me to pure functions!
"Well, it lead me to pure functional programming, and we will see why it is superior to an object-oriented approach when we want to express stateful computations.
****)

## What you can expect from this article

Kurzer Überblick - warum machen wir das?

Worum geht es: Um einen Weg, Signalflüsse in einer dafür passenden Form - ohne Boilerplate, etc - zu beschreiben. "and it tuens out that a functional language called F# brings all the flexibility to "tune" itself in a way we can do this".

Bedingungen:
* Write small, easy to understand pieces of code (let's call them "blocks")
* We want to be able to use these blocks in a larger context, means by composing them.
* It shall then be able to use the results of the composition as blocks themselves, that are again composable, and so on.


## Abgrenzung

* Audio signals (means: at the end, we are interested in a sequence of float values)
* Time domain only
* Performance: Not considered here.

## A Brief Definition of DSP

Digital signal processing - in contrast to analog signal processing - deals with quantized values over a discrete time. What does it mean: Take an analog synthesizer. It creates and outputs a signal based on electric currency - which is continuous from a physical point of view. A computer cannot process values in a continuous way - it has to *quantize* two things:
    * Time: This is called sampling, and happens for an audio signal usually at multiples of 44100 times per second (44.1 kHz). Why 44100? Look at TODO: Sample Theorem.
    * Values: At each sample point, a value must be captured (analog to the electric currency). This happens usually in a number, represented by a 16, 32 or 64 bit value. In this article, we use a F# "float" value, which is a 64 bit floating point number.

Having understood the definition from above, it's easy to define what a signal is: **A signal is a value that changes over time**. Sampling of these values with constant time intervals results in a sequence of values:

TODO: Plot

As we can see, we have captured a sine wave with the *amplitude* of 0.5 (see here TODO) and a frequence of 1Hz (1 complete cycle in 1s). The sample rate is 16Hz. Why? Because we have captured 16 samples in 1 second.

Given the information that we have a sample rate of 16Hz, we can simplify our time-value sequence to just a value sequence:
[ 0.0; 0.3; ...]

The point-in-time of the the n-th value in the sequence can easily be calculated when sample rate and starting time are given. This fact is fundamental, and leads to a definition of what *processing* means:

<statement>Processing signal is creating or changing sequence of values.</statement>

### Constraints:

* Real Time: Meistens will man Effekte oder erzeugte Klänge sofort hören - z.B. wenn man live spielt oder an seinem Computer-Musik Programm sitzt. D.h. wir sind nicht an Funktionen interessiert, die auf einer kompletten Sequenz arbeiten oder diese generieren, sondern an Funktionen, die einen Ausschnitt oder sogar nur **Einzelwerte** verarbeiten. So we are interested in functions that operate on these values. Beispiel: Live-Musik und Effektgerät: Nicht zuerst alles einspielen und dann den Effekt drüberlegen. Das alles soll quasi "sofort" passieren mit einer sehr geringen Latenz (Def: Latenz).

We treat a 'sequence of values' like a stream instead of a random access persistent array, so that when we talk about signal processing, we mean 'steam processing' in this article.

* Composable (nicht immer als Ganzes schreiben) and reusable

* [input signals]: normalized floats with range of -1..1 --> oder 0..1? TODO

* Signale sind symmetrisch (schwingen um die Null-Linie). Das muss oft beachtet werden, wenn wir Algorithmen programmieren.

* Mono

## Goals

TODO



## Nachvollziehen: Die Beispiele können in VS Code / Ionide, F# Interactive nachvollzogen werden.

TODO



