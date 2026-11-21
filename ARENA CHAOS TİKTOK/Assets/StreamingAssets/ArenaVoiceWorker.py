import base64
from contextlib import redirect_stdout
from pathlib import Path
import sys

protocol = sys.stdout


def reply(message):
    print(message, file=protocol, flush=True)


def report(error):
    detail = str(error).encode("utf-8")
    reply("ERR:" + base64.b64encode(detail).decode("ascii"))


def run(runtime):
    sys.path.insert(0, str(runtime))
    import soundfile as sf
    import torch
    from openvoice.api import OpenVoiceBaseClass, ToneColorConverter

    torch.set_num_threads(4)
    converter = ToneColorConverter.__new__(ToneColorConverter)
    OpenVoiceBaseClass.__init__(
        converter, str(runtime / "converter" / "config.json"), device="cpu"
    )
    converter.watermark_model = None
    converter.load_ckpt(str(runtime / "converter" / "checkpoint.pth"))
    reference = converter.extract_se(str(runtime / "reference.wav"))
    reply("READY")

    for request in sys.stdin:
        output = None
        try:
            source = Path(base64.b64decode(request.strip(), validate=True).decode("utf-8"))
            output = source.with_suffix(".converted.wav")
            samples = converter.convert(
                str(source), converter.extract_se(str(source)), reference
            )
            rate = converter.hps.data.sampling_rate
            if not len(samples) or not (abs(samples).max() > 0.001):
                raise ValueError("Voice conversion produced no audible speech")
            sf.write(output, samples, rate, subtype="PCM_16")
            output.replace(source)
        except Exception as error:
            report(error)
        else:
            reply("OK")
        finally:
            if output is not None:
                output.unlink(missing_ok=True)


if __name__ == "__main__":
    try:
        with redirect_stdout(sys.stderr):
            run(Path(sys.argv[1]).resolve())
    except Exception as error:
        report(error)
        sys.exit(1)
