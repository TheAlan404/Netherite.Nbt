namespace fNbt {
    enum NbtParseState {
        AtStreamBeginning,
        AtValue,
        InCompound,
        InList,
        AtCompoundEnd,
        AtStreamEnd,
        Error
    }
}