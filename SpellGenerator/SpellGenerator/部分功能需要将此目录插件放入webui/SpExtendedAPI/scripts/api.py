import lora
from modules.generation_parameters_copypaste import infotext_to_setting_name_mapping
from modules import sd_vae
from modules.api import api
from pydantic import BaseModel, Field
from typing import Optional, List


infotext_to_setting_name_mapping.append(('SD VAE', 'sd_vae'))

class LoraItem(BaseModel):
    name: str = Field(title="Name")

class VaeItem(BaseModel):
    name: str = Field(title="Name")

class ApiHijack(api.Api):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.add_api_route("/sp/loras", self.sp_loras, methods=["GET"], response_model=List[LoraItem])
        self.add_api_route("/sp/vaes", self.sp_vaes, methods=["GET"], response_model=List[VaeItem])

    def sp_loras(self):
        return [{"name": x[0]} for x in lora.available_loras.items()]

    def sp_vaes(self):
        return [{"name": name} for name in sd_vae.vae_dict]

api.Api = ApiHijack
