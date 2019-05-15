
<!-- Reading this, you might think that DSP fits perfectly in the world of object-oriented programming languages: With it's basic idea of encapsulating mutable state by behavior (methods) and giving it an addressable identity (instance pointer), it maps directly to the concept of having physical entities (like condensors or transistors) that "exist" and hold their's own state that changes over time. This article is about an alternative approach of audio-DSP which is solely based on the paradigm of pure functions using a functional language like F#. -->


```TODO: wegmachen
## Goals of this Articl

* Composable (nicht immer als Ganzes schreiben) and reusable
* [input signals]: normalized floats with range of -1..1  oder 0..1? TODO
* Signale sind symmetrisch (schwingen um die Null-Linie). Das muss oft beachtet werden, wenn wir Algorithmen programmieren.
* Mono
```



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



aus 3:

<!-- 
TODO:
    * Kondensator modellieren mit Rückkopplung
    * Rückkopplung ist "intern" - Kasten drum; black box
    * Dann: Verwendung

OOP:
    * Es ist ok, das so mit mutable zu schreiben.
    * Aber: Die Verwendung ist doof, weil: Wir _brauchen_ eine Referenz.
        * Identity in imperative lang is made by an address. Accessing the address is made by a name.
        * BlockDiag: Identity (of the concrete LP filter instance) is made by it's location in the computation.
-->

