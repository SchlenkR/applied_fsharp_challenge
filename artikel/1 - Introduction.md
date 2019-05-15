
## Introduction

Making music with the help of computers is not a new approach. It started with [MUSIC / MUSIC-N](https://en.wikipedia.org/wiki/MUSIC-N) language family back in the 1950s, that were already capable of synthesizing sounds. There are a lot of graphical tools like [PureData](https://puredata.info/), in which the user can put together signal flows by dragging pre-defined objects onto a canvas and connect their inputs / outputs. Some tools are focused on controlling, composition and live performance, like [Sonic PI](https://sonic-pi.net/) or [Tidal](https://tidalcycles.org/index.php/Welcome), while others can do both, like [SuperCollider](https://supercollider.github.io/). There are languages and tools involving interesting concepts to make work with (audio and control) signals easy, like [ChucK](http://chuck.cs.princeton.edu/), that has an interesting way of dealing with time. And of course, there are the widely used multi-paradigm languages like C or C++, where the user can do basically everything - as long as he knows how to do it. And there is way more interesting stuff out there, for example the JavaScript library [genish.js / gibberish](http://www.charlie-roberts.com/genish/), that aims to provide ways where a user can define a syntax tree that is translated into high performance code.

As developing software is my profession and my hobby, I wanted to understand why there are so many different tools and languages out there, and I started my own work in C#, since this is my main language I work in, with a focus on sample-based generation and processing of audio signals (the code is still available [here](https://archive.codeplex.com/?p=byond), but not maintained anymore). But I soon began to realize that there are disadvantages over the special purpose languages around there. I had to deal with aspects that hadn't anything to do with describing signal flows, and I had to write a lot more boilerplate code. That all smelled like [accidental complexity](https://en.wikipedia.org/wiki/No_Silver_Bullet), and I asked myself, how I could find abstractions that would help me writing code that focussed solely on signal processing.

So after evaluating several languages, I decided to go for it using F#, mainly due to it's "in-built" flexibility without dealing with macros or other hardcore meta programming, and of course due to the fact that I'm quite familiar with .NET, but that turned out to be not important at all. One personal thing I can state for myself is: I learned a lot, and never regretted my choice of using F#.

<!-- We will learn that object-oriented concept like instances (and therfore instanciation) as well as aspects of method evaluation can be left out completely, leading to a way of describing signal flows and circuits purely focussing on how values flow through the system.

Key point: Eigentlich denkt man: OO,  ...this leads to - well, it lead me to pure functions!
"Well, it lead me to pure functional programming, and we will see why it is superior to an object-oriented approach when we want to express stateful computations. -->

<!--
## What you can expect from this article and what not?

Bedingungen:
* Write small, easy to understand pieces of code (let's call them "blocks")
* We want to be able to use these blocks in a larger context, means by composing them.
* It shall then be able to use the results of the composition as blocks themselves, that are again composable, and so on.

## Abgrenzung

* Audio signals (means: at the end, we are interested in a sequence of float values)
* Time domain only
* Performance: Not considered here.
-->

## What is DSP?

### A Brief Definition

In the analog world, physical quantities like the electric currency are used to represent a signal that is finally sent to a speaker. Thes quantities are created and altered by low-level components like condensors, resistors, magnetic coils, transistors, diods, etc. which are connected to each others in circuits. They are composed to larger components like operational amplifiers, that are again used to build modules like filters and oscillators which synthesizers and effect processors are made of. Digital audio signal processing (Audio-DSP) is about modeling these components on different levels of abstraction, resulting in the simulation of circuits similar to their analog counterparts (of course, there is no limit in creating completely new forms of synthesizers and effects).

<!-- Reading this, you might think that DSP fits perfectly in the world of object-oriented programming languages: With it's basic idea of encapsulating mutable state by behavior (methods) and giving it an addressable identity (instance pointer), it maps directly to the concept of having physical entities (like condensors or transistors) that "exist" and hold their's own state that changes over time. This article is about an alternative approach of audio-DSP which is solely based on the paradigm of pure functions using a functional language like F#. -->

### Quantization of Time and Values

Digital signal processing - in contrast to analog signal processing - deals with quantized values over a discrete time. What does it mean: Take an analog synthesizer. It creates and outputs a signal based on electric currency - which is continuous from a physical point of view. A computer cannot process values in a continuous way - it has to *quantize* two things:

**Time:**

This is called sampling, and happens for an audio signal usually at multiples of 44100 times per second (44.1 kHz). Why 44100? Have a look at the [Nyquist–Shannon sampling theorem](https://en.wikipedia.org/wiki/Nyquist–Shannon_sampling_theorem) (you don't have to read it to understand this article).

**Values:**

At each sample point, a value must be captured (analog to the electric currency). This happens usually in a number, represented by a 16, 32 or 64 bit value. In this article, we use a F# "float" value, which is a 64 bit floating point number.

Having understood the definition from above, it's easy to define what a signal is:

<statement>A signal is a value that changes over time.</statement>

Sampling of these values with constant time intervals results in a sequence of values:

![Sin Wave](./sinus_wave.png)

As we can see, we have captured a sine wave with the *amplitude* of 0.5 and a frequence of ca. 150Hz (assuming the x-scale is milliseconds, then we have: 3 cycles in 0.02s = 150Hz). The sample rate is 1kHz. Why? Because we have captured 20 samples in 0.02s. That makes 1000 samples in 1s. This is 1kHz.

Given the information that we have a sample rate of 16Hz, we can simplify our time-value sequence to just a value sequence:

```fsharp
[ 0.0; 0.4047782464; 0.4752436455; 0.1531976936; -0.2953766911; -0.499994728; (*and so on *) ]
```

The point-in-time of the the n-th value in the sequence can easily be calculated when sample rate and starting time are given. This fact is fundamental, and leads to a definition of what *processing* means:

<statement>DSP is about creating or changing sequence of values.</statement>

That sounds very general - and it indeed is! The techniques that are introduced here have basically no specialization in terms of "sound" or "audio" - even if they fit well.

### Real Time

"Real Time" originally means: A system is able to react in a predefined timespan. That doesn't necessarily mean it has to be "fast" in the context of the problem, but only that it is reliable in terms of reaction time. Since making music is mostly something that has a "live" character, this is a huge constraint that has impact on how computer music is made. An example: You composed a nice synth line, now you want to apply your hand-written dictortion effect on it. While it is playing, you want to tune parameters of your effect - and you expect to hear a change in sound _immediately_. Immediately usually means some 10 to 100 ms. When that timespan is longer, IMHO, it's not that fun anymore and also harder because you are missing this direct feedback of your action.

As a consequence, we have to design systems that work on a per-sample basis instead of having random access to the whole input sequence. This means, it is not possible to wait until the whole input signal (the synth line) is available, and then apply our effect by mapping values. That amount of latency wouldn't be acceptable for the _most_ use cases. So signal processing can be seen as some kind of *stream processing*.

```TODO: wegmachen
## Goals of this Articl

* Composable (nicht immer als Ganzes schreiben) and reusable
* [input signals]: normalized floats with range of -1..1  oder 0..1? TODO
* Signale sind symmetrisch (schwingen um die Null-Linie). Das muss oft beachtet werden, wenn wir Algorithmen programmieren.
* Mono
```

## Setup / Reproduce the Samples

In this article, a lot of code is presented. Since no one has to beleive that the things I'm telling here are true, you can easily reproduce and comprehend them by your own.

I recommend an easy setup, consisting of the following tools:

* **Visual Studio Code**
  
  VS Code is a lightweight and free to use editor. Get it [here](https://code.visualstudio.com/)!

* **Ionide**
  
  Ionide is a VS Code package suite for cross platform F# development. You can get it [here](http://ionide.io/).
  Note that on the Ionide homepage, there is also explained how you can install F# for your platform (macOS, Windows, Linux).

There is a [github repository](https://github.com/ronaldschlenker/challenge) that you can clone (or view online) with all the samples included.
