# Banking77

Customer service queries from the banking domain, each labelled with one of 77 intents.

| File | Rows | |
|---|---|---|
| `train.csv` | 10,003 | `text,category`; used to compute intent centroids |
| `test.csv` | 3,080 | `text,category`; held out for evaluation (40 per intent) |
| `categories.json` | 77 | intent labels |

Source: [PolyAI-LDN/task-specific-datasets](https://github.com/PolyAI-LDN/task-specific-datasets) (`banking_data/`),
licensed under [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/). The files are unmodified.

> Casanueva, I., Temčinas, T., Gerz, D., Henderson, M., Vulić, I. (2020).
> *Efficient Intent Detection with Dual Sentence Encoders.* Proceedings of the 2nd Workshop on NLP for
> Conversational AI. arXiv:2003.04807.

Quirks handled on import (not edited in the files):

- Two labels are spelled inconsistently: `Refund_not_showing_up` and `reverted_card_payment?`. Intent ids are
  canonicalized (lowercased, non `[a-z0-9_]` characters removed) to `refund_not_showing_up` and `reverted_card_payment`.
- A few texts repeat within a split (4 in train, 1 in test after normalizing) and are stored once.
- 7 texts appear in both train and test; each split keeps its own row.
